namespace Application.Dtos
{
    public record AddEndPointRequestDto(string Url, List<string> SupportedResources, Guid InstitutionId, Guid TestingPatientId);
    public record FHIREndpointDto(Guid Id, string Url, List<ResourceStatusDto> Resources);
    public record ResourceStatusDto(string ResourceName, bool IsVerified, string? ErrorMessage);

}
