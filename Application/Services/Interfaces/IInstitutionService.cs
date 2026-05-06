using Application.Dtos;
using Domain.Enums;

namespace Application.Services.Interfaces
{
    public interface IInstitutionService
    {
        Task<BaseResponse<InstitutionDto>> GetInstitutionByIdAsync(Guid id);
        Task<BaseResponse<IEnumerable<InstitutionDto>>> GetAllInstitutionsAsync();
        Task<BaseResponse<bool>> UpdateInstitutionStatusAsync(Guid id, VerificationStatus status);
        Task<BaseResponse<IEnumerable<GetInstitutionDto>>> GetAllVerifiedInstitutionsAsync();
    }
}
