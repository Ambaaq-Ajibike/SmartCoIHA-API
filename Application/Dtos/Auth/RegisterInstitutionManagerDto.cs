namespace Application.Dtos.Auth
{
    public record RegisterInstitutionManagerDto(
        string Email,
        string FullName,
        string Password,
        string ConfirmPassword,
        string InstitutionName,
        string InstitutionAddress,
        string InstitutionRegistrationId
    );
}