using FCG.Games.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Games.Infrastructure.Persistence.Configurations;

public class OrderGameConfiguration : IEntityTypeConfiguration<OrderGame>
{
    public void Configure(EntityTypeBuilder<OrderGame> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.UserId).IsRequired();
        builder.Property(o => o.GameId).IsRequired();
        builder.Property(o => o.Price).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Currency).HasMaxLength(10).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.CorrelationId).HasMaxLength(200);
    }
}
