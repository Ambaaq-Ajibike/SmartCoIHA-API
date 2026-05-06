namespace Application.Dtos
{
    public record RegisterPatientDto(string Name, string Email, Guid InstitutionId, string InstitutePatientId);
    public record PatientDto(string InstitutionPatientId, string Name, string Email, string Institution, string EnrollmentStatus);
    public record AddFingerprintDto(string PatientId, string FingerprintTemplate);


    public record BulkUploadResultDto(
        int TotalRecords,
        int SuccessCount,
        int FailedCount,
        List<string> Errors);
    public record PatientCsvDto(string ID, string Name, string Email);
}
