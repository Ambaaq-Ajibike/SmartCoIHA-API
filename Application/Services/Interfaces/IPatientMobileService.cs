using Application.Dtos;

namespace Application.Services.Interfaces
{
    public interface IPatientMobileService
    {
        Task<BaseResponse<PatientAuthResponseDto>> VerifyIdentityAsync(VerifyPatientIdentityDto dto);
        Task<BaseResponse<PatientAuthResponseDto>> PatientLoginAsync(PatientLoginDto dto);
        Task<BaseResponse<PatientDto>> GetPatientProfileAsync(Guid patientId, Guid institutionId);
        Task<BaseResponse<IEnumerable<PatientDataRequestHistoryDto>>> GetDataRequestHistoryAsync(Guid patientId, Guid institutionId);
    }
}
