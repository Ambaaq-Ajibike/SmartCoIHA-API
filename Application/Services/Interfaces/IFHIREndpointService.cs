using Application.Dtos;

namespace Application.Services.Interfaces
{
    public interface IFHIREndpointService
    {
        Task<BaseResponse<Guid>> AddEndpointAsync(AddEndPointRequestDto request);
        Task<BaseResponse<FHIREndpointDto>> GetEndpointByInstitutionIdAsync(Guid institutionId);

    }
}
