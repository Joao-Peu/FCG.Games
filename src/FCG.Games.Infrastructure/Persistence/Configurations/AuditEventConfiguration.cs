using FCG.Games.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Games.Infrastructure.Persistence.Configurations;

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.HasKey(a => a.EventId);
        builder.Property(a => a.EntityName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.EntityKey).HasMaxLength(200);
        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
    }
}
