using Application.Dtos;

namespace Application.Services.Interfaces
{
    public interface IAnalyticsService
    {
        Task<BaseResponse<AdminDashboardDto>> GetAdminDashboardAsync();
        Task<BaseResponse<InstitutionDashboardDto>> GetInstitutionDashboardAsync(Guid institutionId);
    }
}