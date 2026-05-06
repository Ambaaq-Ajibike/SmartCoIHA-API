namespace Application.Dtos.Auth
{
    public record VerifyEmailDto(
        string Email,
        string Token
    );
}