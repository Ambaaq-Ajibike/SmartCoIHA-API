using Application.Dtos;

namespace Application.Repositories.Interfaces
{
    public interface IAnalyticsRepository
    {
        Task<AdminDashboardDto> GetAdminSummaryAsync();
        Task<InstitutionDashboardDto> GetInstitutionSummaryAsync(Guid institutionId);
    }
}