// We are re-using your existing file to avoid orphans, but you can rename this file to "AnalyticsDto.cs" in your IDE.
namespace Application.Dtos
{
    public record GraphDataDto(
        List<string> Labels,
        List<GraphDatasetDto> Datasets
    );

    public record GraphDatasetDto(
        string Label,
        List<int> Data
    );

    public record PieChartDataDto(
        List<string> Labels,
        List<int> Data
    );

    public record AuditLogDto(
        Guid Id,
        string ActionType,
        string EntityName,
        string Timestamp,
        string UserName
    );

    public record AdminDashboardDto(
        int TotalInstitutions,
        int TotalPatients,
        int TotalDataRequests,
        int ActiveEndpoints,
        PieChartDataDto InstitutionStatusDistribution,
        GraphDataDto MonthlyRegistrations,
        IEnumerable<AuditLogDto> RecentActivityLogs
    );

    public record InstitutionDashboardDto(
        int TotalPatients,
        int TotalVerifiedPatients,
        int TotalPendingPatients,
        int TotalDataRequests,
        int IncomingDataRequests,
        int OutgoingDataRequests,
        PieChartDataDto PatientVerificationDistribution,
        GraphDataDto MonthlyDataRequests
    );
}