using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence.Configurations;

// Keyed by the hash itself: a refresh looks an old token up by it, and two refreshes retiring the same token at once
// collide on the key (PK_RetiredRefreshTokens). Rows go with their session (cascade), so the session cleanup
// removes them too.
internal sealed class RetiredRefreshTokenConfiguration : IEntityTypeConfiguration<RetiredRefreshToken>
{
    public void Configure(EntityTypeBuilder<RetiredRefreshToken> builder)
    {
        builder.ToTable("RetiredRefreshTokens", IdentityTables.Schema);
        builder.HasKey(x => x.TokenHash);
        builder.Property(x => x.TokenHash).HasMaxLength(32).IsFixedLength(); // SHA-256
        builder.Property(x => x.RetiredAtUtc).IsUtcTimestamp();
    }
}
