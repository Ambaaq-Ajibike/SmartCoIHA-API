using Application.Dtos;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Application.Services.Implementations
{
    public class PatientMobileService(
        IGenericRepository<Patients> _patientRepository,
        IGenericRepository<Institution> _institutionRepository,
        IGenericRepository<DataRequest> _dataRequestRepository,
        IConfiguration _configuration,
        ILogger<PatientMobileService> _logger) : IPatientMobileService
    {
        public async Task<BaseResponse<PatientAuthResponseDto>> VerifyIdentityAsync(VerifyPatientIdentityDto dto)
        {
            _logger.LogInformation("Verifying patient identity for InstitutePatientId: {PatientId}, InstitutionId: {InstitutionId}",
                dto.InstitutePatientId, dto.InstitutionId);

            var validator = new VerifyPatientIdentityValidator();
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Identity verification validation failed: {Errors}", errors);
                return new BaseResponse<PatientAuthResponseDto>(false, errors, null!);
            }

            var patient = await _patientRepository.GetByExpressionAsync(
                p => p.InstitutePatientId == dto.InstitutePatientId && p.InstitutionID == dto.InstitutionId);

            if (patient == null)
            {
                _logger.LogWarning("Patient not found for InstitutePatientId: {PatientId}, InstitutionId: {InstitutionId}",
                    dto.InstitutePatientId, dto.InstitutionId);
                return new BaseResponse<PatientAuthResponseDto>(false, "Patient not found.", null!);
            }

            if (!string.Equals(patient.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Email mismatch for patient {PatientId}. Provided: {ProvidedEmail}", dto.InstitutePatientId, dto.Email);
                return new BaseResponse<PatientAuthResponseDto>(false, "The email provided does not match our records.", null!);
            }

            var institution = await _institutionRepository.GetByIdAsync(patient.InstitutionID);
            var token = GeneratePatientJwtToken(patient, institution);

            _logger.LogInformation("Patient identity verified successfully for {PatientId}", dto.InstitutePatientId);

            return new BaseResponse<PatientAuthResponseDto>(true, "Identity verified successfully.", new PatientAuthResponseDto(
                token,
                patient.ID,
                patient.InstitutePatientId,
                patient.Name,
                patient.Email,
                institution.Id,
                institution.Name));
        }

        public async Task<BaseResponse<PatientAuthResponseDto>> PatientLoginAsync(PatientLoginDto dto)
        {
            _logger.LogInformation("Patient login attempt for InstitutePatientId: {PatientId}, InstitutionId: {InstitutionId}",
                dto.InstitutePatientId, dto.InstitutionId);

            var validator = new PatientLoginValidator();
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Patient login validation failed: {Errors}", errors);
                return new BaseResponse<PatientAuthResponseDto>(false, errors, null!);
            }

            var patient = await _patientRepository.GetByExpressionAsync(
                p => p.InstitutePatientId == dto.InstitutePatientId && p.InstitutionID == dto.InstitutionId);

            if (patient == null)
            {
                _logger.LogWarning("Patient not found for login: {PatientId}", dto.InstitutePatientId);
                return new BaseResponse<PatientAuthResponseDto>(false, "Patient not found.", null!);
            }

            if (string.IsNullOrWhiteSpace(patient.FingerPrint))
            {
                _logger.LogWarning("Patient {PatientId} has no enrolled fingerprint.", dto.InstitutePatientId);
                return new BaseResponse<PatientAuthResponseDto>(false, "Fingerprint not enrolled. Please complete enrollment first.", null!);
            }

            var isValid = PatientService.VerifyFingerprintTemplate(dto.FingerprintTemplate, patient.FingerPrint, patient.ID);

            if (!isValid)
            {
                _logger.LogWarning("Fingerprint verification failed for patient {PatientId}", dto.InstitutePatientId);
                return new BaseResponse<PatientAuthResponseDto>(false, "Fingerprint verification failed.", null!);
            }

            var institution = await _institutionRepository.GetByIdAsync(patient.InstitutionID);
            var token = GeneratePatientJwtToken(patient, institution);

            _logger.LogInformation("Patient login successful for {PatientId}", dto.InstitutePatientId);

            return new BaseResponse<PatientAuthResponseDto>(true, "Login successful.", new PatientAuthResponseDto(
                token,
                patient.ID,
                patient.InstitutePatientId,
                patient.Name,
                patient.Email,
                institution.Id,
                institution.Name));
        }

        public async Task<BaseResponse<PatientDto>> GetPatientProfileAsync(Guid patientId, Guid institutionId)
        {
            _logger.LogInformation("Retrieving profile for patient ID: {PatientId} and institution ID: {InstitutionId}", patientId, institutionId);

            var patient = await _patientRepository.GetByExpressionAsync(p => p.ID == patientId && p.InstitutionID == institutionId);

            if (patient == null)
            {
                _logger.LogWarning("Patient not found for ID: {PatientId}", patientId);
                return new BaseResponse<PatientDto>(false, "Patient not found.", null!);
            }

            var institution = await _institutionRepository.GetByIdAsync(patient.InstitutionID);

            return new BaseResponse<PatientDto>(true, "Profile retrieved successfully.", new PatientDto(
                patient.InstitutePatientId,
                patient.Name,
                patient.Email,
                institution?.Name ?? "Unknown",
                patient.EnrollmentStatus.ToString()));
        }

        public async Task<BaseResponse<IEnumerable<PatientDataRequestHistoryDto>>> GetDataRequestHistoryAsync(Guid patientId, Guid institutionId)
        {
            _logger.LogInformation("Retrieving data request history for patient ID: {PatientId} and institution ID: {InstitutionId}", patientId, institutionId);

            var patient = await _patientRepository.GetByIdAsync(patientId);

            if (patient == null)
            {
                _logger.LogWarning("Patient not found for ID: {PatientId}", patientId);
                return new BaseResponse<IEnumerable<PatientDataRequestHistoryDto>>(false, "Patient not found.", []);
            }

            var dataRequests = await _dataRequestRepository.GetAllAsync(
                dr => dr.InstitutePatientId == patient.InstitutePatientId && dr.PatientInstitutionId == institutionId);

            var historyList = new List<PatientDataRequestHistoryDto>();

            foreach (var request in dataRequests)
            {
                var requestingInstitution = await _institutionRepository.GetByIdAsync(request.RequestingInstitutionId);
                var isExpired = request.IsExpired();
                var status = ComputeRequestStatus(request.InstitutionApprovedStatus, request.FingerprintValidationSuccess, isExpired);

                historyList.Add(new PatientDataRequestHistoryDto(
                    request.Id,
                    requestingInstitution?.Name ?? "Unknown",
                    request.ResourceType,
                    request.RequestedTimestamp,
                    request.ExpiryTimestamp,
                    request.InstitutionApprovedStatus.ToString(),
                    request.FingerprintValidationSuccess,
                    isExpired,
                    status));
            }

            var ordered = historyList.OrderByDescending(h => h.RequestedTimestamp).ToList();

            _logger.LogInformation("Retrieved {Count} data request(s) for patient {PatientId}", ordered.Count, patientId);

            return new BaseResponse<IEnumerable<PatientDataRequestHistoryDto>>(
                true,
                $"{ordered.Count} data request(s) retrieved.",
                ordered);
        }

        private static string ComputeRequestStatus(VerificationStatus institutionStatus, bool patientApproved, bool isExpired)
        {
            if (isExpired) return "Expired";
            if (institutionStatus == VerificationStatus.Denied) return "Denied by Institution";
            if (institutionStatus == VerificationStatus.Pending) return "Awaiting Institution Review";
            if (institutionStatus == VerificationStatus.Verified && !patientApproved) return "Awaiting Your Approval";
            if (institutionStatus == VerificationStatus.Verified && patientApproved) return "Approved & Shared";
            return "Unknown";
        }

        private string GeneratePatientJwtToken(Patients patient, Institution institution)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, patient.ID.ToString()),
                new(ClaimTypes.Email, patient.Email),
                new(ClaimTypes.Name, patient.Name),
                new(ClaimTypes.Role, "Patient"),
                new("PatientId", patient.ID.ToString()),
                new("InstitutePatientId", patient.InstitutePatientId),
                new("InstitutionId", institution.Id.ToString()),
                new("InstitutionName", institution.Name)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
