using Application.Dtos;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations
{
    public class DataRequestService(
        IGenericRepository<DataRequest> _dataRequestRepository,
        IGenericRepository<Patients> _patientRepository,
        IGenericRepository<InstituteBaserUrl> _endpointRepository,
        IGenericRepository<Institution> _institutionRepository,
        ICacheService _cacheService,
        INotificationService _notificationService,
        ILogger<DataRequestService> _logger) : IDataRequestService
    {
        public async Task<BaseResponse<Guid>> MakeDataRequestAsync(MakeDataRequestDto dataRequestDto)
        {
            _logger.LogInformation("Attempting to create Data Request. RequestingInstitutionId: {RequestingInstitutionId}, PatientId: {InstitutePatientId}, ResourceType: {ResourceType}",
                dataRequestDto.RequestingInstitutionId, dataRequestDto.InstitutePatientId, dataRequestDto.ResourceType);

            // Validate using FluentValidation
            var validator = new MakeDataRequestValidator();
            var validationResult = await validator.ValidateAsync(dataRequestDto);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Data Request validation failed: {ValidationErrors}", errors);
                return new BaseResponse<Guid>(false, errors, Guid.Empty);
            }

            // Validate requesting institution exists
            var requestingInstitution = await _institutionRepository.GetByIdAsync(dataRequestDto.RequestingInstitutionId);
            if (requestingInstitution == null)
            {
                _logger.LogWarning("Data request failed. Requesting Institution not found for ID: {RequestingInstitutionId}", dataRequestDto.RequestingInstitutionId);
                return new BaseResponse<Guid>(
                    false,
                    "Requesting institution not found.",
                    Guid.Empty);
            }
            // Validate patient exists
            var patientInstitution = await _institutionRepository.GetByExpressionAsync(x => x.Id == dataRequestDto.PatientInstituteId);
            if (patientInstitution == null)
            {
                _logger.LogWarning("Data request failed. Patient Institution not found for ID: {InstitutePatientId}", dataRequestDto.PatientInstituteId);
                return new BaseResponse<Guid>(
                    false,
                    "Patient Institution not found.",
                    Guid.Empty);
            }

            // Validate patient exists
            var patient = await _patientRepository.GetByExpressionAsync(p => p.InstitutePatientId == dataRequestDto.InstitutePatientId && p.InstitutionID == dataRequestDto.PatientInstituteId);
            if (patient == null)
            {
                _logger.LogWarning("Data request failed. Patient not found for ID: {InstitutePatientId}", dataRequestDto.InstitutePatientId);
                return new BaseResponse<Guid>(
                    false,
                    "Patient not found.",
                    Guid.Empty);
            }

            // Validate patient is enrolled and verified
            if (patient.EnrollmentStatus != VerificationStatus.Verified)
            {
                _logger.LogWarning("Data request failed. Patient {InstitutePatientId} enrollment is not verified. Current Status: {EnrollmentStatus}",
                    dataRequestDto.InstitutePatientId, patient.EnrollmentStatus);
                return new BaseResponse<Guid>(
                    false,
                    "Patient enrollment is not verified. Data request cannot be made. Download the app to verify patient",
                    Guid.Empty);
            }

            // Create data request
            var dataRequest = new DataRequest(
                dataRequestDto.RequestingInstitutionId,
                patient.InstitutionID,
                dataRequestDto.InstitutePatientId,
                dataRequestDto.ResourceType);

            var createdRequest = await _dataRequestRepository.AddAsync(dataRequest);
            await _dataRequestRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully created Data Request: {DataRequestId}", createdRequest.Id);

            // Send notification to patient
            await _notificationService.CreateNotificationAsync(
                patient.ID,
                "New Data Request",
                $"{requestingInstitution.Name} has requested access to your {dataRequestDto.ResourceType} records.",
                NotificationType.DataRequestCreated,
                createdRequest.Id);

            return new BaseResponse<Guid>(
                true,
                "Data request created successfully. Awaiting institution approval and patient fingerprint verification.",
                createdRequest.Id);
        }

        public async Task<BaseResponse<IEnumerable<DataRequestDto>>> GetDataRequestsForInstitutionAsync(Guid institutionId)
        {
            _logger.LogInformation("Attempting to get data requests for Institution ID: {InstitutionId}", institutionId);

            // Validate institution exists
            var institution = await _institutionRepository.GetByIdAsync(institutionId);
            if (institution == null)
            {
                _logger.LogWarning("Failed to retrieve data requests. Institution ID: {InstitutionId} not found.", institutionId);
                return new BaseResponse<IEnumerable<DataRequestDto>>(
                    false,
                    $"Institution not found.",
                    []);
            }

            // Get all data requests where patients belong to this institution
            var allRequests = await _dataRequestRepository.GetAllAsync(dr => dr.RequestingInstitutionId == institutionId);

            var requestDtos = new List<DataRequestDto>();

            foreach (var request in allRequests)
            {
                var patient = await _patientRepository.GetByExpressionAsync(p => p.InstitutePatientId.ToLower() == request.InstitutePatientId.ToLower());

                // Filter requests for patients belonging to this institution
                if (patient != null && patient.InstitutionID == institutionId)
                {
                    var requestingInstitution = await _institutionRepository.GetByIdAsync(request.RequestingInstitutionId);

                    var dto = new DataRequestDto(
                        request.Id,
                        requestingInstitution?.Name ?? "Unknown Institution",
                        request.InstitutePatientId,
                        request.ResourceType,
                        request.FingerprintValidationSuccess,
                        request.InstitutionApprovedStatus.ToString(),
                        request.IsExpired(),
                        request.RequestedTimestamp.ToString("yyyy-MM-dd HH:mm:ss")
                        );

                    requestDtos.Add(dto);
                }
            }

            if (requestDtos.Count == 0)
            {
                _logger.LogInformation("No data requests found for Institution ID: {InstitutionId}", institutionId);
                return new BaseResponse<IEnumerable<DataRequestDto>>(
                    true,
                    "No data requests found for this institution.",
                    []);
            }

            _logger.LogInformation("Successfully retrieved {DataRequestCount} data request(s) for Institution ID: {InstitutionId}", requestDtos.Count, institutionId);
            return new BaseResponse<IEnumerable<DataRequestDto>>(
                true,
                $"{requestDtos.Count} data request(s) retrieved successfully.",
                requestDtos);
        }

        public async Task<BaseResponse<bool>> UpdateInstitutionApprovalStatusAsync(Guid requestId, string newStatus)
        {
            _logger.LogInformation("Attempting to update status for Data Request: {DataRequestId} to {VerificationStatus}", requestId, newStatus);

            if (!Enum.TryParse<VerificationStatus>(newStatus, true, out var parsedStatus))
            {
                _logger.LogWarning("Status update failed for Data Request: {DataRequestId}. Invalid Status Format: {VerificationStatus}", requestId, newStatus);
                return new BaseResponse<bool>(
                    false,
                    "Invalid status format. Please provide a valid status.",
                    false);
            }
            // Validate status

            if (parsedStatus != VerificationStatus.Verified && parsedStatus != VerificationStatus.Denied)
            {
                _logger.LogWarning("Status update failed for Data Request: {DataRequestId}. Invalid Status: {VerificationStatus}", requestId, newStatus);
                return new BaseResponse<bool>(
                    false,
                    "Invalid status. Only 'Verified' or 'Rejected' statuses are allowed.",
                    false);
            }

            // Get data request
            var dataRequest = await _dataRequestRepository.GetByIdAsync(requestId);
            if (dataRequest == null)
            {
                _logger.LogWarning("Status update failed. Data Request: {DataRequestId} not found.", requestId);
                return new BaseResponse<bool>(
                    false,
                    $"Data request not found.",
                    false);
            }

            // Check if request is expired
            if (dataRequest.IsExpired())
            {
                _logger.LogWarning("Status update failed. Data Request: {DataRequestId} is expired.", requestId);
                return new BaseResponse<bool>(
                    false,
                    "Data request has expired and cannot be updated.",
                    false);
            }

            // Update approval status
            await dataRequest.UpdateInstitutionApprovalStatus(parsedStatus);
            _dataRequestRepository.Update(dataRequest);
            await _dataRequestRepository.SaveChangesAsync();

            var statusText = parsedStatus == VerificationStatus.Verified ? "approved" : "rejected";
            _logger.LogInformation("Successfully updated Data Request: {DataRequestId} to {VerificationStatus}", requestId, newStatus);

            // Send notification to patient
            var patient = await _patientRepository.GetByExpressionAsync(p => p.InstitutePatientId == dataRequest.InstitutePatientId);
            if (patient != null)
            {
                var requestingInstitution = await _institutionRepository.GetByIdAsync(dataRequest.RequestingInstitutionId);
                var institutionName = requestingInstitution?.Name ?? "An institution";

                if (parsedStatus == VerificationStatus.Verified)
                {
                    await _notificationService.CreateNotificationAsync(
                        patient.ID,
                        "Request Approved — Your Fingerprint Needed",
                        $"The request for your {dataRequest.ResourceType} by {institutionName} has been approved by your institution. Your fingerprint is needed to complete the process.",
                        NotificationType.InstitutionApproved,
                        requestId);
                }
                else
                {
                    await _notificationService.CreateNotificationAsync(
                        patient.ID,
                        "Data Request Denied",
                        $"The request for your {dataRequest.ResourceType} by {institutionName} has been denied by your institution.",
                        NotificationType.InstitutionDenied,
                        requestId);
                }
            }

            return new BaseResponse<bool>(
                true,
                $"Data request {statusText} successfully.",
                true);
        }

        public async Task<BaseResponse<bool>> VerifyPatientFingerprintAsync(Guid requestId, string institutePatientId, string fingerprintTemplate)
        {
            _logger.LogInformation("Attempting to verify fingerprint for Data Request: {DataRequestId}, PatientId: {InstitutePatientId}", requestId, institutePatientId);

            // Validate fingerprint template
            if (string.IsNullOrWhiteSpace(fingerprintTemplate))
            {
                _logger.LogWarning("Fingerprint verification failed. Empty template provided for Data Request: {DataRequestId}", requestId);
                return new BaseResponse<bool>(
                    false,
                    "Fingerprint template is required.",
                    false);
            }

            // Get data request
            var dataRequest = await _dataRequestRepository.GetByIdAsync(requestId);
            if (dataRequest == null)
            {
                _logger.LogWarning("Fingerprint verification failed. Data Request: {DataRequestId} not found.", requestId);
                return new BaseResponse<bool>(
                    false,
                    "Data request not found.",
                    false);
            }

            // Check if request is expired
            if (dataRequest.IsExpired())
            {
                _logger.LogWarning("Fingerprint verification failed. Data Request: {DataRequestId} is expired.", requestId);
                return new BaseResponse<bool>(
                    false,
                    "Data request has expired. Fingerprint verification not allowed.",
                    false);
            }

            // Get patient
            var patient = await _patientRepository.GetByExpressionAsync(p => p.InstitutePatientId.ToLower() == institutePatientId && p.InstitutionID == dataRequest.PatientInstitutionId);
            if (patient == null)
            {
                _logger.LogWarning("Fingerprint verification failed. Patient: {InstitutePatientId} not found.", institutePatientId);
                return new BaseResponse<bool>(
                    false,
                    "Patient not found.",
                    false);
            }

            // Validate patient ID matches request
            if (dataRequest.InstitutePatientId != patient.InstitutePatientId)
            {
                _logger.LogWarning("Fingerprint verification failed. Provided Patient: {InstitutePatientId} does not match request Patient: {RequestPatientId}",
                    institutePatientId, dataRequest.InstitutePatientId);
                return new BaseResponse<bool>(
                    false,
                    "Patient ID does not match the data request.",
                    false);
            }

            // Check if patient has fingerprint registered
            if (string.IsNullOrWhiteSpace(patient.FingerPrint))
            {
                _logger.LogWarning("Fingerprint verification failed. Patient: {InstitutePatientId} has no registered fingerprint.", institutePatientId);
                return new BaseResponse<bool>(
                    false,
                    "Patient does not have a registered fingerprint. Please register fingerprint first.",
                    false);
            }

            // Verify the fingerprint template against stored hash
            bool isValid = PatientService.VerifyFingerprintTemplate(
                fingerprintTemplate,
                patient.FingerPrint,
                patient.ID);

            if (!isValid)
            {
                _logger.LogWarning("Fingerprint verification failed due to mismatch for Patient: {InstitutePatientId}, Data Request: {DataRequestId}", institutePatientId, requestId);
                return new BaseResponse<bool>(
                    false,
                    "Fingerprint verification failed. The provided fingerprint does not match the registered fingerprint.",
                    false);
            }

            // Update fingerprint validation result
            await dataRequest.UpdateFingerprintValidationResult(true);
            _dataRequestRepository.Update(dataRequest);
            await _dataRequestRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully verified fingerprint and approved access for Data Request: {DataRequestId}", requestId);

            // Send notification to patient
            var requestingInstitution = await _institutionRepository.GetByIdAsync(dataRequest.RequestingInstitutionId);
            await _notificationService.CreateNotificationAsync(
                patient.ID,
                "Access Approved",
                $"You have approved access to your {dataRequest.ResourceType} records for {requestingInstitution?.Name ?? "the requesting institution"}.",
                NotificationType.PatientApproved,
                requestId);

            return new BaseResponse<bool>(
                true,
                "Patient fingerprint verified successfully. Data request is now approved for access.",
                true);
        }

        public async Task<BaseResponse<Resource>> GetPatientResourceDataAsync(Guid requestId)
        {
            _logger.LogInformation("Attempting to get resource data for Data Request: {DataRequestId}", requestId);

            // Get data request
            var dataRequest = await _dataRequestRepository.GetByIdAsync(requestId);
            if (dataRequest == null)
            {
                _logger.LogWarning("Failed to get resource data. Data Request: {DataRequestId} not found.", requestId);
                return new BaseResponse<Resource>(
                    false,
                    "Data request not found.",
                    null);
            }

            // Check if request has expired
            if (dataRequest.IsExpired())
            {
                _logger.LogWarning("Failed to get resource data. Data Request: {DataRequestId} has expired.", requestId);
                return new BaseResponse<Resource>(
                    false,
                    "Data request has expired. Please create a new request.",
                    null);
            }

            // Check if institution has approved the request
            if (dataRequest.InstitutionApprovedStatus != VerificationStatus.Verified)
            {
                _logger.LogWarning("Failed to get resource data. Data Request: {DataRequestId} has status: {InstitutionApprovedStatus}",
                    requestId, dataRequest.InstitutionApprovedStatus);

                var statusMessage = dataRequest.InstitutionApprovedStatus == VerificationStatus.Denied
                    ? "Data request was rejected by the institution."
                    : "Data request is pending institution approval.";

                return new BaseResponse<Resource>(
                    false,
                    statusMessage,
                    null);
            }

            // Check if patient fingerprint has been validated
            if (!dataRequest.FingerprintValidationSuccess)
            {
                _logger.LogWarning("Failed to get resource data. Fingerprint validation is missing for Data Request: {DataRequestId}", requestId);
                return new BaseResponse<Resource>(
                    false,
                    "Patient fingerprint validation is required before accessing data.",
                    null);
            }

            var patient = await _patientRepository.GetByExpressionAsync(p => p.InstitutePatientId == dataRequest.InstitutePatientId);
            if (patient == null)
            {
                _logger.LogWarning("Failed to get resource data. Patient: {InstitutePatientId} not found.", dataRequest.InstitutePatientId);
                return new BaseResponse<Resource>(
                    false,
                    "Patient not found.",
                    null);
            }

            // Get patient's institution to retrieve FHIR endpoint
            var institution = await _institutionRepository.GetByIdAsync(patient.InstitutionID);
            if (institution == null)
            {
                _logger.LogWarning("Failed to get resource data. Institution ID: {InstitutionId} not found for Patient: {InstitutePatientId}",
                    patient.InstitutionID, patient.InstitutePatientId);
                return new BaseResponse<Resource>(
                    false,
                    "Patient's institution not found.",
                    null);
            }

            // Generate cache key based on patient ID and resource type
            var cacheKey = $"fhir_resource:{dataRequest.InstitutePatientId}:{dataRequest.ResourceType.ToLower()}";

            // Check if data exists in cache
            var cachedJson = await _cacheService.GetAsync(cacheKey);
            if (cachedJson != null)
            {
                try
                {
                    _logger.LogInformation("Cache hit for resource query. Key: {CacheKey}", cacheKey);
                    var parser = new FhirJsonParser();
                    var cachedResource = parser.Parse<Resource>(cachedJson);
                    return new BaseResponse<Resource>(
                        true,
                        "Patient resource data retrieved successfully from cache.",
                        cachedResource);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize cached FHIR resource. Evicting stale cache entry. Key: {CacheKey}", cacheKey);
                    await _cacheService.RemoveAsync(cacheKey);
                }
            }

            _logger.LogInformation("Cache miss for resource query. Proceeding to fetch from FHIR endpoint for Key: {CacheKey}", cacheKey);

            // Get FHIR endpoint for the institution
            var (IsSuccess, BaseUrl, ErrorMessage) = await GetInstitutionFhirEndpointAsync(patient.InstitutionID);
            if (!IsSuccess)
            {
                _logger.LogWarning("Failed to resolve FHIR endpoint for Institution ID: {InstitutionId}. Error: {ErrorMessage}", patient.InstitutionID, ErrorMessage);
                return new BaseResponse<Resource>(
                    false,
                    ErrorMessage!,
                    null);
            }

            // Fetch resource data from FHIR endpoint
            try
            {
                _logger.LogInformation("Fetching external FHIR data. ResourceType: {ResourceType}, BaseUrl: {BaseUrl}, PatientId: {PatientId}",
                    dataRequest.ResourceType, BaseUrl, dataRequest.InstitutePatientId);

                var resourceData = await FetchFhirResourceDataAsync(
                    BaseUrl!,
                    dataRequest.ResourceType,
                    dataRequest.InstitutePatientId);

                if (resourceData is null)
                {
                    _logger.LogInformation("External FHIR fetch returned no data for ResourceType: {ResourceType}, PatientId: {PatientId}",
                        dataRequest.ResourceType, dataRequest.InstitutePatientId);

                    return new BaseResponse<Resource>(
                        false,
                        $"No {dataRequest.ResourceType} data found for the patient.",
                       null);
                }

                // Cache the resource data as FHIR JSON for 2 hours
                var cacheExpiration = TimeSpan.FromHours(2);
                var serializer = new FhirJsonSerializer();
                var resourceJson = serializer.SerializeToString(resourceData);
                await _cacheService.SetRawAsync(cacheKey, resourceJson, cacheExpiration);

                _logger.LogInformation("Successfully fetched and cached FHIR resource data. Source Cached: {CacheKey}", cacheKey);

                return new BaseResponse<Resource>(
                    true,
                    "Patient resource data retrieved successfully.",
                    resourceData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching external FHIR data. DataRequestId: {DataRequestId}", requestId);
                return new BaseResponse<Resource>(
                    false,
                    $"Error retrieving data from FHIR endpoint: {ex.Message}",
                    null);
            }
        }

        public async Task<BaseResponse<IEnumerable<DataRequestDto>>> GetOutgoingDataRequestsAsync(Guid institutionId)
        {
            _logger.LogInformation("Attempting to get outgoing data requests for Institution ID: {InstitutionId}", institutionId);

            var institution = await _institutionRepository.GetByIdAsync(institutionId);
            if (institution == null)
            {
                _logger.LogWarning("Failed to retrieve outoing data requests. Institution ID: {InstitutionId} not found.", institutionId);
                return new BaseResponse<IEnumerable<DataRequestDto>>(false, "Institution not found.", []);
            }

            // Get requests made BY this institution
            var allRequests = await _dataRequestRepository.GetAllAsync(dr => dr.RequestingInstitutionId == institutionId);
            var requestDtos = new List<DataRequestDto>();

            foreach (var request in allRequests)
            {
                // the target is the patient's institution
                var patientInstitution = await _institutionRepository.GetByIdAsync(request.PatientInstitutionId);

                requestDtos.Add(new DataRequestDto(
                    request.Id,
                    patientInstitution?.Name ?? "Unknown Institution",
                    request.InstitutePatientId,
                    request.ResourceType,
                    request.FingerprintValidationSuccess,
                    request.InstitutionApprovedStatus.ToString(),
                    request.IsExpired(),
                    request.RequestedTimestamp.ToString("yyyy-MM-dd HH:mm:ss")
                ));
            }

            _logger.LogInformation("Successfully retrieved {DataRequestCount} outgoing data request(s) for Institution ID: {InstitutionId}", requestDtos.Count, institutionId);
            return new BaseResponse<IEnumerable<DataRequestDto>>(true, $"{requestDtos.Count} outgoing data request(s) retrieved successfully.", requestDtos);
        }

        public async Task<BaseResponse<IEnumerable<DataRequestDto>>> GetIncomingDataRequestsAsync(Guid institutionId)
        {
            _logger.LogInformation("Attempting to get incoming data requests for Institution ID: {InstitutionId}", institutionId);

            var institution = await _institutionRepository.GetByIdAsync(institutionId);
            if (institution == null)
            {
                _logger.LogWarning("Failed to retrieve incoming data requests. Institution ID: {InstitutionId} not found.", institutionId);
                return new BaseResponse<IEnumerable<DataRequestDto>>(false, "Institution not found.", []);
            }

            // Get requests made TO this institution (where the patient is registered)
            var allRequests = await _dataRequestRepository.GetAllAsync(dr => dr.PatientInstitutionId == institutionId);
            var requestDtos = new List<DataRequestDto>();

            foreach (var request in allRequests)
            {
                var requestingInstitution = await _institutionRepository.GetByIdAsync(request.RequestingInstitutionId);

                requestDtos.Add(new DataRequestDto(
                    request.Id,
                    requestingInstitution?.Name ?? "Unknown Institution",
                    request.InstitutePatientId,
                    request.ResourceType,
                    request.FingerprintValidationSuccess,
                    request.InstitutionApprovedStatus.ToString(),
                    request.IsExpired(),
                    request.RequestedTimestamp.ToString("yyyy-MM-dd HH:mm:ss")
                ));
            }

            _logger.LogInformation("Successfully retrieved {DataRequestCount} incoming data request(s) for Institution ID: {InstitutionId}", requestDtos.Count, institutionId);
            return new BaseResponse<IEnumerable<DataRequestDto>>(true, $"{requestDtos.Count} incoming data request(s) retrieved successfully.", requestDtos);
        }
        private async Task<(bool IsSuccess, string? BaseUrl, string? ErrorMessage)> GetInstitutionFhirEndpointAsync(Guid institutionId)
        {
            var endpoint = await _endpointRepository.GetByExpressionAsync(e =>
                e.InstitutionID == institutionId &&
                e.VerificationStatus == VerificationStatus.Verified);

            if (endpoint is null)
            {
                return (false, null, "No verified FHIR endpoint found for the patient's institution.");
            }

            return (true, endpoint.Url, null);
        }

        private static async Task<Resource> FetchFhirResourceDataAsync(
            string baseUrl,
            string resourceType,
            string patientId)
        {
            var settings = new FhirClientSettings
            {
                Timeout = 30000, // 30 seconds
                PreferredFormat = ResourceFormat.Json,
                VerifyFhirVersion = false
            };

            var client = new FhirClient(baseUrl, settings);

            // Fetch data based on resource type
            var resourceData = resourceType.ToLower() switch
            {
                "patient" => await FetchPatientDataAsync(client, patientId),
                "observation" => await FetchObservationDataAsync(client, patientId),
                "condition" => await FetchConditionDataAsync(client, patientId),
                "medicationrequest" => await FetchMedicationRequestDataAsync(client, patientId),
                "diagnosticreport" => await FetchDiagnosticReportDataAsync(client, patientId),
                "procedure" => await FetchProcedureDataAsync(client, patientId),
                "encounter" => await FetchEncounterDataAsync(client, patientId),
                "allergyintolerance" => await FetchAllergyIntoleranceDataAsync(client, patientId),
                "immunization" => await FetchImmunizationDataAsync(client, patientId),
                "careplan" => await FetchCarePlanDataAsync(client, patientId),
                "goal" => await FetchGoalDataAsync(client, patientId),
                "documentreference" => await FetchDocumentReferenceDataAsync(client, patientId),
                _ => await FetchGenericResourceDataAsync(client, resourceType, patientId)
            };

            return resourceData;
        }

        private static async Task<Patient> FetchPatientDataAsync(FhirClient client, string patientId)
        {
            var patient = await client.ReadAsync<Patient>($"Patient/{patientId}");
            return patient;
        }

        private static async Task<Bundle> FetchObservationDataAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<Observation>([$"patient={patientId}", "_count=100"]);
            return bundle;
        }

        private static async Task<Bundle> FetchConditionDataAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<Condition>([$"patient={patientId}", "_count=100"]);
            return bundle;
        }

        private static async Task<Bundle> FetchMedicationRequestDataAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<MedicationRequest>([$"patient={patientId}", "_count=100"]);
            return bundle;
        }

        private static async Task<Bundle> FetchDiagnosticReportDataAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<DiagnosticReport>([$"patient={patientId}", "_count=100"]);
            return bundle;
        }

        private static async Task<Bundle> FetchProcedureDataAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<Procedure>([$"patient={patientId}", "_count=100"]);
            return bundle;
        }

        private static async Task<Bundle> FetchEncounterDataAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<Encounter>([$"patient={patientId}", "_count=100"]);
            return bundle;
        }

        private static async Task<Bundle> FetchAllergyIntoleranceDataAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<AllergyIntolerance>([$"patient={patientId}", "_count=100"]);
            return bundle;
        }

        private static async Task<Bundle> FetchImmunizationDataAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<Immunization>([$"patient={patientId}", "_count=100"]);
            return bundle;
        }

        private static async Task<Bundle> FetchCarePlanDataAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<CarePlan>([$"patient={patientId}", "_count=100"]);
            return bundle;
        }

        private static async Task<Bundle> FetchGoalDataAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<Goal>([$"patient={patientId}", "_count=100"]);
            return bundle;
        }

        private static async Task<Bundle> FetchDocumentReferenceDataAsync(FhirClient client, string patientId)
        {
            var bundle = await client.SearchAsync<DocumentReference>([$"patient={patientId}", "_count=100"]);
            return bundle;
        }

        private static async Task<Resource> FetchGenericResourceDataAsync(FhirClient client, string resourceType, string patientId)
        {
            var url = $"{resourceType}?patient={patientId}&_count=100";
            var result = await client.GetAsync(url);
            return result;
        }
    }
}
