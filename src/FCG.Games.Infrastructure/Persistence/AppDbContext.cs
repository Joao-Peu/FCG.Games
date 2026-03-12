using System.Text.Json;
using FCG.Games.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCG.Games.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Game> Games => Set<Game>();
    public DbSet<OrderGame> Orders => Set<OrderGame>();
    public DbSet<UserLibraryEntry> UserLibraryEntries => Set<UserLibraryEntry>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        ChangeTracker.DetectChanges();

        var auditEntries = new List<AuditEvent>();

        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            if (entry.Entity is AuditEvent) continue;

            var audit = new AuditEvent
            {
                EventId = Guid.NewGuid(),
                CreatedAtUtc = now,
                EntityName = entry.Entity.GetType().Name,
                EntityKey = entry.Properties
                    .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString(),
                Action = entry.State.ToString(),
                Data = JsonSerializer.Serialize(entry.CurrentValues.ToObject())
            };
            auditEntries.Add(audit);
        }

        if (auditEntries.Count > 0) AuditEvents.AddRange(auditEntries);

        return await base.SaveChangesAsync(cancellationToken);
    }
}
