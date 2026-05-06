using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace Persistence.Interceptors
{
    public class AuditLogInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var dbContext = eventData.Context;
            if (dbContext is null)
                return await base.SavingChangesAsync(eventData, result, cancellationToken);

            var auditEntries = new List<AuditLog>();

            // Get all tracked entities that have been modified, excluding the AuditLog itself
            var entries = dbContext.ChangeTracker.Entries()
                .Where(e => e.Entity is not AuditLog &&
                            e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            foreach (var entry in entries)
            {
                var auditLog = new AuditLog
                {
                    EntityName = entry.Entity.GetType().Name,
                    ActionType = entry.State.ToString(),
                    Timestamp = DateTime.UtcNow,
                    UserId = httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Unknown"
                };

                // Extract Primary Key
                var primaryKey = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                auditLog.PrimaryKey = primaryKey?.CurrentValue?.ToString() ?? "Unknown";

                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                foreach (var property in entry.Properties)
                {
                    var propertyName = property.Metadata.Name;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            newValues[propertyName] = property.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            oldValues[propertyName] = property.OriginalValue;
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                oldValues[propertyName] = property.OriginalValue;
                                newValues[propertyName] = property.CurrentValue;
                            }
                            break;
                    }
                }

                auditLog.OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null;
                auditLog.NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null;

                auditEntries.Add(auditLog);
            }

            if (auditEntries.Count != 0)
            {
                await dbContext.Set<AuditLog>().AddRangeAsync(auditEntries, cancellationToken);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}