using Application.Dtos;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;

namespace Application.Services.Implementations
{
    public class AnalyticsService(IAnalyticsRepository analyticsRepository) : IAnalyticsService
    {
        public async Task<BaseResponse<AdminDashboardDto>> GetAdminDashboardAsync()
        {
            var dashboard = await analyticsRepository.GetAdminSummaryAsync();
            return new BaseResponse<AdminDashboardDto>(true, "Admin dashboard configured successfully.", dashboard);
        }

        public async Task<BaseResponse<InstitutionDashboardDto>> GetInstitutionDashboardAsync(Guid institutionId)
        {
            var dashboard = await analyticsRepository.GetInstitutionSummaryAsync(institutionId);
            return new BaseResponse<InstitutionDashboardDto>(true, "Institution dashboard configured successfully.", dashboard);
        }
    }
}