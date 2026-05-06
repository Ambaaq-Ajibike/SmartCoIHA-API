using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<DataRequest> DataRequests { get; set; }
        public DbSet<Patients> Patients { get; set; }
        public DbSet<InstituteBaserUrl> FHIREndpoints { get; set; }
        public DbSet<Institution> Institutions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<InstitutionManager> InstitutionManagers { get; set; }
        public DbSet<FhirResourceStatus> FhirResourceStatuses { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<DeviceToken> DeviceTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed Admin User
            // Note: Replace PasswordHash with a valid BCrypt hash matching your default password, e.g. "Admin@123".
            modelBuilder.Entity<User>().HasData(new
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Email = "admin@smartcoiha.com",
                FullName = "System Administrator",
                PasswordHash = "$2a$12$EMIUh4Yb0t/UmHWEIJCkDOlsA5JvjaW6DXYdbS5WFRf/gUTJ7zkI.", // example hash for Administration default password
                Role = Role.Admin,
                IsEmailVerified = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }
    }
}
