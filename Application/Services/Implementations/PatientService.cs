using Application.Dtos;
using Application.Messaging.Interfaces;
using Application.Messaging.Models;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Application.Validators;
using CsvHelper;
using CsvHelper.Configuration;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services.Implementations
{
    public class PatientService(
        IGenericRepository<Patients> _patientRepository,
        IGenericRepository<Institution> _institutionRepository,
        IMessagePublisher _messagePublisher,
        ILogger<PatientService> _logger) : IPatientService
    {
        public async Task<BaseResponse<Guid>> RegsiterPatientAsync(RegisterPatientDto patientDto)
        {
            _logger.LogInformation("Attempting to register patient: {PatientName} ({Email}) for Institution ID: {InstitutionId}",
                patientDto.Name, patientDto.Email, patientDto.InstitutionId);

            var validator = new RegisterPatientValidator();
            var validationResult = await validator.ValidateAsync(patientDto);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Patient registration validation failed for {Email}: {ValidationErrors}", patientDto.Email, errors);
                return new BaseResponse<Guid>(false, errors, Guid.Empty);
            }

            // Verify institution exists
            var institution = await _institutionRepository.GetByIdAsync(patientDto.InstitutionId);
            if (institution == null)
            {
                _logger.LogWarning("Patient registration failed. Institution ID: {InstitutionId} not found.", patientDto.InstitutionId);
                return new BaseResponse<Guid>(false, $"Institution with ID {patientDto.InstitutionId} not found.", Guid.Empty);
            }

            var patient = new Patients(patientDto.Name, patientDto.Email, patientDto.InstitutionId, patientDto.InstitutePatientId);

            var createdPatient = await _patientRepository.AddAsync(patient);
            await _patientRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully registered patient. Patient ID: {PatientId}, InstitutePatientId: {InstitutePatientId}",
                createdPatient.ID, patientDto.InstitutePatientId);

            // Send email notification via RabbitMQ
            await SendEmailNotificationAsync(patientDto, createdPatient.ID.ToString(), institution.Name);
            return new BaseResponse<Guid>(true, "Patient registered successfully.", createdPatient.ID);
        }

        private async Task SendEmailNotificationAsync(RegisterPatientDto patientDto, string createdPatientId, string institutionName)
        {
            _logger.LogInformation("Queueing welcome email notification for Patient ID: {PatientId} to {Email}", createdPatientId, patientDto.Email);

            var emailMessage = new EmailNotificationMessage
            {
                To = patientDto.Email,
                Subject = "Welcome to SmartCoIHA - Patient Registration Successful",
                Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>Welcome to SmartCoIHA</h2>
                        <p>Dear {patientDto.Name},</p>
                        <p>Your registration has been completed successfully.</p>
                        <table style='border-collapse: collapse; margin: 20px 0;'>
                            <tr>
                                <td style='padding: 8px; font-weight: bold;'>Patient ID:</td>
                                <td style='padding: 8px;'>{patientDto.InstitutePatientId}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px; font-weight: bold;'>Institution:</td>
                                <td style='padding: 8px;'>{institutionName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px; font-weight: bold;'>Enrollment Status:</td>
                                <td style='padding: 8px;'>Pending Verification</td>
                            </tr>
                        </table>
                        <p>Your enrollment status is currently pending verification. To complete your enrollment, please open the SmartCoIHA mobile app using the link below:</p>
                        <p style='margin: 20px 0;'>
                            <a href='smartcoiha://onboard?patientId={patientDto.InstitutePatientId}&institutionId={patientDto.InstitutionId}'
                               style='background-color: #065f46; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold;'>
                                Open SmartCoIHA App
                            </a>
                        </p>
                        <p>If the button above does not work, copy and paste this link into your mobile browser:</p>
                        <p style='word-break: break-all; color: #065f46;'>smartcoiha://onboard?patientId={patientDto.InstitutePatientId}&institutionId={patientDto.InstitutionId}</p>
                        <br>
                        <p>Best regards,<br><strong>SmartCoIHA Team</strong></p>
                    </body>
                    </html> "
            };

            await _messagePublisher.PublishAsync(emailMessage, "email-queue");
        }

        public async Task<BaseResponse<PatientDto>> GetPatientByIdAsync(Guid patientId)
        {
            _logger.LogInformation("Attempting to retrieve Patient ID: {PatientId}", patientId);

            var patient = await _patientRepository.GetByIdAsync(patientId);

            if (patient == null)
            {
                _logger.LogWarning("Patient retrieval failed. Patient ID: {PatientId} not found.", patientId);
                return new BaseResponse<PatientDto>(false, $"Patient with ID {patientId} not found.", null!);
            }

            var patientDto = new PatientDto(
                patient.InstitutePatientId,
                patient.Name,
                patient.Email,
                patient.Institution?.Name ?? "Unknown",
                patient.EnrollmentStatus.ToString());

            _logger.LogInformation("Successfully retrieved Patient: {PatientName} ({PatientId})", patient.Name, patientId);
            return new BaseResponse<PatientDto>(true, "Patient retrieved successfully.", patientDto);
        }

        public async Task<BaseResponse<IEnumerable<PatientDto>>> GetPatientsAsync(string? institutionId, VerificationStatus? enrollmentStatus)
        {
            _logger.LogInformation("Attempting to retrieve patients. Filters -> InstitutionId: {InstitutionId}, EnrollmentStatus: {EnrollmentStatus}",
                institutionId ?? "None", enrollmentStatus?.ToString() ?? "None");

            // Build the filter expression dynamically
            Expression<Func<Patients, bool>> filterExpression = p => true;

            if (!string.IsNullOrWhiteSpace(institutionId) && Guid.TryParse(institutionId, out Guid parsedInstitutionId))
            {
                if (enrollmentStatus.HasValue)
                {
                    filterExpression = p => p.InstitutionID == parsedInstitutionId && p.EnrollmentStatus == enrollmentStatus.Value;
                }
                else
                {
                    filterExpression = p => p.InstitutionID == parsedInstitutionId;
                }
            }
            else if (enrollmentStatus.HasValue)
            {
                filterExpression = p => p.EnrollmentStatus == enrollmentStatus.Value;
            }

            var patients = await _patientRepository.GetAllAsync(filterExpression);

            if (!patients.Any())
            {
                _logger.LogInformation("No patients found matching the provided criteria.");
                return new BaseResponse<IEnumerable<PatientDto>>(
                    true,
                    "No patients found matching the criteria.",
                    null!);
            }

            var patientDtos = patients.Select(patient => new PatientDto(
                patient.InstitutePatientId,
                patient.Name,
                patient.Email,
                patient.Institution?.Name ?? "Unknown",
                patient.EnrollmentStatus.ToString()));

            _logger.LogInformation("Successfully retrieved {PatientCount} patient(s).", patients.Count);
            return new BaseResponse<IEnumerable<PatientDto>>(
                true,
                $"{patients.Count} patient(s) retrieved successfully.",
                patientDtos);
        }

        public async Task<BaseResponse<bool>> AddFingerprintAsync(string patientId, string fingerprintTemplate)
        {
            _logger.LogInformation("Attempting to add biometric fingerprint for Patient ID: {PatientId}", patientId);

            // Validate input
            if (string.IsNullOrWhiteSpace(fingerprintTemplate))
            {
                _logger.LogWarning("Fingerprint addition failed. Provided template was empty for Patient ID: {PatientId}", patientId);
                return new BaseResponse<bool>(false, "Fingerprint template cannot be empty.", false);
            }

            // Retrieve patient
            var patient = await _patientRepository.GetByExpressionAsync(p => p.InstitutePatientId == patientId);
            if (patient == null)
            {
                _logger.LogWarning("Fingerprint addition failed. Patient ID: {PatientId} not found.", patientId);
                return new BaseResponse<bool>(false, $"Patient with ID {patientId} not found.", false);
            }

            // Hash the fingerprint template with salt
            var hashedFingerprint = HashFingerprintTemplate(fingerprintTemplate, patient.ID.ToString());

            // Update patient fingerprint
            await patient.UpdateFingerPrint(hashedFingerprint);

            _patientRepository.Update(patient);
            await _patientRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully added and hashed fingerprint for Patient ID: {PatientId}", patientId);
            return new BaseResponse<bool>(true, "Fingerprint added successfully.", true);
        }

        public async Task<BaseResponse<BulkUploadResultDto>> BulkUploadPatientsAsync(IFormFile csvFile, Guid institutionId)
        {
            _logger.LogInformation("Attempting bulk patient upload for Institution ID: {InstitutionId}. File: {FileName} ({FileSize} bytes)",
                institutionId, csvFile?.FileName, csvFile?.Length);

            // Validate institution exists upfront
            var institution = await _institutionRepository.GetByIdAsync(institutionId);
            if (institution == null)
            {
                _logger.LogWarning("Bulk upload failed. Institution ID: {InstitutionId} not found.", institutionId);
                return new BaseResponse<BulkUploadResultDto>(
                    false,
                    $"Institution with ID {institutionId} not found.",
                    new BulkUploadResultDto(0, 0, 0, ["Institution not found"]));
            }

            // Validate file
            var (IsValid, ErrorMessage) = ValidateCsvFile(csvFile);
            if (!IsValid)
            {
                _logger.LogWarning("Bulk upload failed due to file validation: {ErrorMessage}", ErrorMessage);
                return new BaseResponse<BulkUploadResultDto>(
                    false,
                    ErrorMessage,
                    new BulkUploadResultDto(0, 0, 0, [ErrorMessage]));
            }

            // Read and validate CSV
            var csvReadResult = await ReadCsvFileAsync(csvFile!);
            if (!csvReadResult.IsSuccess)
            {
                _logger.LogWarning("Bulk upload failed while reading CSV: {ErrorMessage}", csvReadResult.ErrorMessage);
                return new BaseResponse<BulkUploadResultDto>(
                    false,
                    csvReadResult.ErrorMessage!,
                    new BulkUploadResultDto(0, 0, 0, [csvReadResult.ErrorMessage!]));
            }

            _logger.LogInformation("Successfully read CSV. Found {TotalRecords} records. Starting batch processing.", csvReadResult.Records!.Count);

            // Process patients in batches
            var result = await ProcessPatientBatchesAsync(csvReadResult.Records!, institutionId, institution);

            var message = $"Bulk upload completed: {result.SuccessCount} succeeded, {result.FailedCount} failed out of {result.TotalRecords} records.";
            _logger.LogInformation(message);

            return new BaseResponse<BulkUploadResultDto>(true, message, result);
        }

        private static (bool IsValid, string ErrorMessage) ValidateCsvFile(IFormFile? csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                return (false, "CSV file is empty or not provided.");
            }

            if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Invalid file format. Only CSV files are allowed.");
            }

            return (true, string.Empty);
        }

        private static async Task<(bool IsSuccess, List<PatientCsvDto>? Records, string? ErrorMessage)> ReadCsvFileAsync(IFormFile csvFile)
        {
            try
            {
                using var reader = new StreamReader(csvFile.OpenReadStream());
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null
                });

                // Validate headers
                await csv.ReadAsync();
                csv.ReadHeader();
                var (IsValid, ErrorMessage) = ValidateCsvHeaders(csv.HeaderRecord);
                if (!IsValid)
                {
                    return (false, null, ErrorMessage);
                }

                var records = csv.GetRecords<PatientCsvDto>().ToList();

                if (records.Count == 0)
                {
                    return (false, null, "CSV file contains no records.");
                }

                return (true, records, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Error reading CSV file: {ex.Message}");
            }
        }

        private static (bool IsValid, string ErrorMessage) ValidateCsvHeaders(string[]? headers)
        {
            if (headers == null || headers.Length == 0)
            {
                return (false, "CSV file has no headers.");
            }

            var requiredHeaders = new[] { "ID", "Name", "Email" };
            var missingHeaders = requiredHeaders
                .Where(required => !headers.Any(h => h.Equals(required, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (missingHeaders.Count != 0)
            {
                var missing = string.Join(", ", missingHeaders);
                return (false, $"CSV file is missing required headers: {missing}");
            }

            return (true, string.Empty);
        }

        private async Task<BulkUploadResultDto> ProcessPatientBatchesAsync(
            List<PatientCsvDto> csvRecords,
            Guid institutionId,
            Institution institution)
        {
            var errors = new List<string>();
            var successCount = 0;
            var batchSize = 100;
            var validator = new RegisterPatientValidator();

            for (int i = 0; i < csvRecords.Count; i += batchSize)
            {
                var batch = csvRecords.Skip(i).Take(batchSize).ToList();
                var (SuccessCount, CreatedPatients) = await ProcessPatientBatchAsync(batch, i, institutionId, validator, errors);

                successCount += SuccessCount;

                // Queue email notifications for successful patients
                if (CreatedPatients.Count != 0)
                {
                    QueueEmailNotifications(CreatedPatients, institution);
                }
            }

            var failedCount = csvRecords.Count - successCount;
            return new BulkUploadResultDto(csvRecords.Count, successCount, failedCount, errors);
        }

        private async Task<(int SuccessCount, List<Patients> CreatedPatients)> ProcessPatientBatchAsync(
            List<PatientCsvDto> batch,
            int batchStartIndex,
            Guid institutionId,
            RegisterPatientValidator validator,
            List<string> errors)
        {
            var patientsToAdd = new List<Patients>();

            for (int j = 0; j < batch.Count; j++)
            {
                var record = batch[j];
                var rowNumber = batchStartIndex + j + 2; // +2 for header row and 0-based index

                var validationResult = await ValidatePatientRecordAsync(record, institutionId, validator, rowNumber);
                if (!validationResult.IsValid)
                {
                    errors.Add(validationResult.ErrorMessage!);
                    continue;
                }

                var patient = new Patients(record.Name, record.Email, institutionId, record.ID);
                patientsToAdd.Add(patient);
            }

            // Bulk add patients for this batch
            var createdPatients = new List<Patients>();
            if (patientsToAdd.Count != 0)
            {
                foreach (var patient in patientsToAdd)
                {
                    var createdPatient = await _patientRepository.AddAsync(patient);
                    createdPatients.Add(createdPatient);
                }

                await _patientRepository.SaveChangesAsync();
            }

            return (createdPatients.Count, createdPatients);
        }

        private static async Task<(bool IsValid, string? ErrorMessage)> ValidatePatientRecordAsync(
            PatientCsvDto record,
            Guid institutionId,
            RegisterPatientValidator validator,
            int rowNumber)
        {
            try
            {
                var patientDto = new RegisterPatientDto(record.Name, record.Email, institutionId, record.ID);

                var validationResult = await validator.ValidateAsync(patientDto);
                if (!validationResult.IsValid)
                {
                    var validationErrors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    return (false, $"Row {rowNumber}: {validationErrors}");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Row {rowNumber}: {ex.Message}");
            }
        }

        private void QueueEmailNotifications(List<Patients> patients, Institution institution)
        {
            _ = Task.Run(async () =>
            {
                foreach (var patient in patients)
                {
                    try
                    {
                        var emailMessage = CreateWelcomeEmail(patient, institution);
                        await _messagePublisher.PublishAsync(emailMessage, "email-queue");
                    }
                    catch (Exception ex)
                    {
                        // Using injected logger instead of Console.WriteLine
                        _logger.LogError(ex, "Error enqueueing welcome email for {Email}", patient.Email);
                    }
                }
            });
        }

        private static EmailNotificationMessage CreateWelcomeEmail(Patients patient, Institution institution)
        {
            return new EmailNotificationMessage
            {
                To = patient.Email,
                Subject = "Welcome to SmartCoIHA - Patient Registration Successful",
                Body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Welcome to SmartCoIHA</h2>
                <p>Dear {patient.Name},</p>
                <p>Your registration has been completed successfully through bulk upload.</p>
                <table style='border-collapse: collapse; margin: 20px 0;'>
                    <tr>
                        <td style='padding: 8px; font-weight: bold;'>Patient ID:</td>
                        <td style='padding: 8px;'>{patient.InstitutePatientId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; font-weight: bold;'>Institution:</td>
                        <td style='padding: 8px;'>{institution.Name}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; font-weight: bold;'>Enrollment Status:</td>
                        <td style='padding: 8px;'>Pending Verification</td>
                    </tr>
                </table>
                <p>Your enrollment status is currently pending verification. To complete your enrollment, please open the SmartCoIHA mobile app using the link below:</p>
                <p style='margin: 20px 0;'>
                    <a href='smartcoiha://onboard?patientId={patient.InstitutePatientId}&institutionId={patient.InstitutionID}'
                       style='background-color: #065f46; color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold;'>
                        Open SmartCoIHA App
                    </a>
                </p>
                <p>If the button above does not work, copy and paste this link into your mobile browser:</p>
                <p style='word-break: break-all; color: #065f46;'>smartcoiha://onboard?patientId={patient.InstitutePatientId}&institutionId={patient.InstitutionID}</p>
                <br>
                <p>Best regards,<br><strong>SmartCoIHA Team</strong></p>
            </body>
            </html>"
            };
        }

        /// <summary>
        /// Hashes the fingerprint template using SHA-256 with a salt derived from the patient ID
        /// </summary>
        /// <param name="fingerprintTemplate">The biometric template received from the frontend</param>
        /// <param name="salt">The salt value (using patient ID for uniqueness)</param>
        /// <returns>The hashed fingerprint string</returns>
        private static string HashFingerprintTemplate(string fingerprintTemplate, string salt)
        {
            // Combine the template with the salt
            var saltedTemplate = $"{salt}:{fingerprintTemplate}";

            // Convert to bytes
            var bytes = Encoding.UTF8.GetBytes(saltedTemplate);

            // Compute SHA-256 hash
            var hashBytes = SHA256.HashData(bytes);

            // Convert to Base64 string for storage
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Verifies a fingerprint template against the stored hash
        /// </summary>
        /// <param name="fingerprintTemplate">The template to verify</param>
        /// <param name="storedHash">The stored hash from database</param>
        /// <param name="patientId">The patient ID used as salt</param>
        /// <returns>True if the template matches the stored hash</returns>
        public static bool VerifyFingerprintTemplate(string fingerprintTemplate, string storedHash, Guid patientId)
        {
            var computedHash = HashFingerprintTemplate(fingerprintTemplate, patientId.ToString());
            return computedHash == storedHash;
        }
    }
}