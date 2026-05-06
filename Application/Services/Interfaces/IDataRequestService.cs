using Application.Dtos;
using Hl7.Fhir.Model;

namespace Application.Services.Interfaces
{
    public interface IDataRequestService
    {
        Task<BaseResponse<Guid>> MakeDataRequestAsync(MakeDataRequestDto dataRequestDto);
        Task<BaseResponse<IEnumerable<DataRequestDto>>> GetOutgoingDataRequestsAsync(Guid institutionId);
        Task<BaseResponse<IEnumerable<DataRequestDto>>> GetIncomingDataRequestsAsync(Guid institutionId);
        Task<BaseResponse<bool>> UpdateInstitutionApprovalStatusAsync(Guid requestId, string newStatus);
        Task<BaseResponse<bool>> VerifyPatientFingerprintAsync(Guid requestId, string institutePatientId, string FingerprintTemplate);
        Task<BaseResponse<Resource>> GetPatientResourceDataAsync(Guid requestId);
    }
}
