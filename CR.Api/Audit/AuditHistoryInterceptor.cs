using System.Text.Json;
using ESG.Api.Audit;
using ESG.Model.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CR.Api.Audit;

public sealed class AuditHistoryInterceptor(IServiceProvider serviceProvider)
    : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        AddAuditLogs(eventData.Context!);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        AddAuditLogs(eventData.Context!);
        return ValueTask.FromResult(result);
    }

    private void AddAuditLogs(DbContext context)
    {
        using var scope = _serviceProvider.CreateScope();
        var auditProvider = scope.ServiceProvider.GetRequiredService<IAuditContextProvider>();
        var audit = auditProvider.Get();

        var entries = context
            .ChangeTracker.Entries()
            .Where(e =>
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                && e.Entity is not AuditLog
            )
            .ToList();

        foreach (var entry in entries)
        {
            var entityName = entry.Metadata.ClrType.Name;
            var entityId = GetPrimaryKey(entry);

            var log = new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = entry.State.ToString(),
                OccurredAtUtc = audit.UtcNow,
                UserId = string.IsNullOrEmpty(audit.UserId) ? null : IdType.Parse(audit.UserId),
                CorrelationId = audit.CorrelationId,
                ChangesJson = JsonSerializer.Serialize(
                    AuditChangeExtractor.Extract(entry),
                    new JsonSerializerOptions { WriteIndented = false }
                ),
            };

            context.Set<AuditLog>().Add(log);
        }
    }

    private static string GetPrimaryKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey()!;

        return string.Join(
            ",",
            key.Properties.Select(p =>
                entry.Property(p.Name).CurrentValue ?? entry.Property(p.Name).OriginalValue
            )
        );
    }
}
