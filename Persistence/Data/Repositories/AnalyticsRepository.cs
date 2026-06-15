using Application.Dtos;
using Application.Repositories.Interfaces;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Persistence.Data.Repositories
{
    public class AnalyticsRepository(ApplicationDbContext dbContext) : IAnalyticsRepository
    {
        public async Task<AdminDashboardDto> GetAdminSummaryAsync()
        {
            var totalInstitutions = await dbContext.Institutions.CountAsync();
            var totalPatients = await dbContext.Patients.CountAsync();
            var totalDataRequests = await dbContext.DataRequests.CountAsync();
            var activeEndpoints = await dbContext.FHIREndpoints.CountAsync();

            var statusGroups = await dbContext.Institutions
                .GroupBy(i => i.VerificationStatus)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var institutionStatusDistribution = new PieChartDataDto(
                [.. statusGroups.Select(g => g.Status)],
                [.. statusGroups.Select(g => g.Count)]
            );

            // Using standard labels. For real-world usage, align with specific time periods and dbContext properties like CreateDate
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
            var startPeriod = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var months = Enumerable.Range(0, 6)
                .Select(i => startPeriod.AddMonths(i))
                .ToList();

            var monthLabels = months.Select(m => m.ToString("MMM")).ToList();

            var monthlyRegistrations = new GraphDataDto(
                monthLabels,
                [
                    new GraphDatasetDto("Institutions", [1, 2, 4, 3, 5, 2]),
                    new GraphDatasetDto("Patients", [10, 20, 15, 30, 25, 40])
                ]
            );

            var recentActivityLogs = await dbContext.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .Select(a => new AuditLogDto(
                    a.Id,
                    a.ActionType,
                    a.EntityName,
                    a.Timestamp.ToString("o"),
                    a.UserId ?? "UnKnown"
                ))
                .ToListAsync();

            return new AdminDashboardDto(
                totalInstitutions,
                totalPatients,
                totalDataRequests,
                activeEndpoints,
                institutionStatusDistribution,
                monthlyRegistrations,
                recentActivityLogs
            );
        }

        public async Task<InstitutionDashboardDto> GetInstitutionSummaryAsync(Guid institutionId)
        {
            var patients = await dbContext.Patients
                .Where(p => p.InstitutionID == institutionId)
                .ToListAsync();

            var totalPatients = patients.Count;
            var totalVerifiedPatients = patients.Count(p => p.EnrollmentStatus == VerificationStatus.Verified);
            var totalPendingPatients = patients.Count(p => p.EnrollmentStatus == VerificationStatus.Pending);

            var patientStatusDistribution = new PieChartDataDto(
                ["Verified", "Pending", "Rejected"],
                new List<int> {
                    totalVerifiedPatients,
                    totalPendingPatients,
                    patients.Count(p => p.EnrollmentStatus == VerificationStatus.Denied)
                }
            );

            var incomingRequests = await dbContext.DataRequests.CountAsync(d => d.PatientInstitutionId == institutionId);
            var outgoingRequests = await dbContext.DataRequests.CountAsync(d => d.RequestingInstitutionId == institutionId);

            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startPeriod = currentMonth.AddMonths(-5);
            var endPeriod = currentMonth.AddMonths(1);

            var months = Enumerable.Range(0, 6)
                .Select(i => startPeriod.AddMonths(i))
                .ToList();

            var monthlyRequestGroups = await dbContext.DataRequests
                .Where(d =>
                    (d.PatientInstitutionId == institutionId || d.RequestingInstitutionId == institutionId)
                    && d.RequestedTimestamp >= startPeriod
                    && d.RequestedTimestamp < endPeriod)
                .GroupBy(d => new { d.RequestedTimestamp.Year, d.RequestedTimestamp.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Incoming = g.Count(d => d.PatientInstitutionId == institutionId),
                    Outgoing = g.Count(d => d.RequestingInstitutionId == institutionId)
                })
                .ToListAsync();

            var monthlyRequestsByMonth = monthlyRequestGroups.ToDictionary(
                g => (g.Year, g.Month),
                g => g);

            var monthLabels = months
                .Select(m => m.ToString("MMM yyyy", CultureInfo.InvariantCulture))
                .ToList();

            var incomingMonthlyData = months
                .Select(m => monthlyRequestsByMonth.TryGetValue((m.Year, m.Month), out var group) ? group.Incoming : 0)
                .ToList();

            var outgoingMonthlyData = months
                .Select(m => monthlyRequestsByMonth.TryGetValue((m.Year, m.Month), out var group) ? group.Outgoing : 0)
                .ToList();

            var monthlyDataRequests = new GraphDataDto(
                monthLabels,
                [
                    new GraphDatasetDto("Incoming Requests", incomingMonthlyData),
                    new GraphDatasetDto("Outgoing Requests", outgoingMonthlyData)
                ]
            );

            return new InstitutionDashboardDto(
                totalPatients,
                totalVerifiedPatients,
                totalPendingPatients,
                incomingRequests + outgoingRequests,
                incomingRequests,
                outgoingRequests,
                patientStatusDistribution,
                monthlyDataRequests
            );
        }
    }
}
