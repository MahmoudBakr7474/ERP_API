using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    private const int HashLength = 32; // SHA-256

    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions", IdentityTables.Schema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.DeviceId).HasMaxLength(100);
        builder.Property(x => x.TokenHash).HasMaxLength(HashLength).IsFixedLength().IsRequired();
        builder.Property(x => x.RefreshTokenHash).HasMaxLength(HashLength).IsFixedLength();
        builder.Property(x => x.PreviousRefreshTokenHash).HasMaxLength(HashLength).IsFixedLength();
        foreach (var time in new[]
                 {
                     nameof(UserSession.TokenExpiresAtUtc), nameof(UserSession.RefreshExpiresAtUtc), nameof(UserSession.CreatedAtUtc),
                     nameof(UserSession.LastSeenAtUtc), nameof(UserSession.ExpiresAtUtc), nameof(UserSession.RevokedAtUtc),
                 })
            builder.Property(time).IsUtcTimestamp();

        // Every authenticated request looks its session up by token hash; a refresh by the current or previous
        // refresh-token hash (the previous one to detect reuse).
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.RefreshTokenHash).IsUnique().HasFilter("[RefreshTokenHash] IS NOT NULL");
        builder.HasIndex(x => x.PreviousRefreshTokenHash).HasFilter("[PreviousRefreshTokenHash] IS NOT NULL");

        builder.HasMany(x => x.RetiredRefreshTokens).WithOne().HasForeignKey(r => r.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.RetiredRefreshTokens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
