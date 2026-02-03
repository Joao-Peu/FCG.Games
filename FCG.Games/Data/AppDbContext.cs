using Microsoft.EntityFrameworkCore;
using FCG.Games.Models;

namespace FCG.Games.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Game> Games { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<AuditEvent> AuditEvents { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        ChangeTracker.DetectChanges();

        var auditEntries = new List<AuditEvent>();

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            if (entry.Entity is AuditEvent) continue;

            var audit = new AuditEvent
            {
                EventId = Guid.NewGuid(),
                CreatedAtUtc = now,
                EntityName = entry.Entity.GetType().Name,
                EntityKey = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString(),
                Action = entry.State.ToString(),
                Data = System.Text.Json.JsonSerializer.Serialize(entry.CurrentValues.ToObject())
            };
            auditEntries.Add(audit);
        }

        if (auditEntries.Any()) AuditEvents.AddRange(auditEntries);

        return await base.SaveChangesAsync(cancellationToken);
    }
}