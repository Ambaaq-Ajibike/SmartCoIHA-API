using Application.Dtos;
using Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Application.Services.Interfaces
{
    public interface IPatientService
    {
        Task<BaseResponse<Guid>> RegsiterPatientAsync(RegisterPatientDto patientDto);
        Task<BaseResponse<PatientDto>> GetPatientByIdAsync(Guid patientId);
        Task<BaseResponse<IEnumerable<PatientDto>>> GetPatientsAsync(string? institutionId, VerificationStatus? enrollmentStatus);
        Task<BaseResponse<bool>> AddFingerprintAsync(string patientId, string fingerprintTemplate);
        Task<BaseResponse<BulkUploadResultDto>> BulkUploadPatientsAsync(IFormFile csvFile, Guid institutionId);
    }
}
