namespace Application.Dtos
{
    public record MakeDataRequestDto(Guid RequestingInstitutionId, Guid PatientInstituteId, string InstitutePatientId, string ResourceType);
    public record DataRequestDto(Guid RequestId, string PatientInstituteName, string InstitutePatientId, string ResourceType, bool HasPatientApproved, string InstitutionApprovalStatus, bool HasExpired, string RequestedTimestamp);
    public record UpdateApprovalStatusDto(string Status);
    public record VerifyFingerprintDto(string FingerprintTemplate);
}
