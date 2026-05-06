using Domain.Enums;

namespace Application.Dtos
{
    public record RegisterInstitutionDto(string Name, string Address, string RegistrationId);
    public record InstitutionDto(Guid Id, string Name, string Address, string RegistrationId, string Status);
    public record GetInstitutionDto(Guid Id, string Name);
    public record UpdateInstitutionStatusDto(VerificationStatus Status);

}
