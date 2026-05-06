using Application.Dtos;
using Application.Repositories.Interfaces;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

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
            var startPeriod = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

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

            var months = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
            var monthlyDataRequests = new GraphDataDto(
                months,
                [
                    new GraphDatasetDto("Incoming Requests", [5, 10, 15, 10, 20, 25]),
                    new GraphDatasetDto("Outgoing Requests", [2, 4, 3, 5, 4, 6])
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