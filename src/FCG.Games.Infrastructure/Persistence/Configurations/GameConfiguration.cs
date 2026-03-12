using FCG.Games.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Games.Infrastructure.Persistence.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Title).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(2000);
        builder.Property(g => g.Genre).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Price).HasColumnType("decimal(18,2)");
        builder.Property(g => g.Currency).HasMaxLength(10).IsRequired();
    }
}
