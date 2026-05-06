namespace Application.Dtos
{
    public record VerifyPatientIdentityDto(string InstitutePatientId, Guid InstitutionId, string Email);

    public record PatientLoginDto(string InstitutePatientId, Guid InstitutionId, string FingerprintTemplate);

    public record PatientAuthResponseDto(
        string Token,
        Guid PatientId,
        string InstitutePatientId,
        string Name,
        string Email,
        Guid InstitutionId,
        string InstitutionName);

    public record PatientDataRequestHistoryDto(
        Guid RequestId,
        string RequestingInstitutionName,
        string ResourceType,
        DateTime RequestedTimestamp,
        DateTime ExpiryTimestamp,
        string InstitutionApprovalStatus,
        bool PatientApproved,
        bool IsExpired,
        string Status);

    public record RegisterDeviceTokenDto(Guid PatientId, string DeviceToken, string Platform);

    public record PatientNotificationDto(
        Guid Id,
        string Title,
        string Message,
        string Type,
        bool IsRead,
        DateTime CreatedAt,
        Guid? DataRequestId);
}
