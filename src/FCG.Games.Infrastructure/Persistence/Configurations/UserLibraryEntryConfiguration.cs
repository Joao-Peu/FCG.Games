using FCG.Games.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Games.Infrastructure.Persistence.Configurations;

public class UserLibraryEntryConfiguration : IEntityTypeConfiguration<UserLibraryEntry>
{
    public void Configure(EntityTypeBuilder<UserLibraryEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.UserId, e.GameId }).IsUnique();
    }
}
