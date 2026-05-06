namespace Application.Dtos.Auth
{
    public record AuthResponseDto(
        string Token,
        string Email,
        string FullName,
        string Role,
        bool IsEmailVerified,
        bool IsInstitutionVerified,
        Guid? InstitutionId,
        string? InstitutionName
    );
}