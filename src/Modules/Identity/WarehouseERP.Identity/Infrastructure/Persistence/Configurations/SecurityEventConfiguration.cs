using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence.Configurations;

// The security audit trail: append-only, not temporal (rows never change), no foreign keys (it outlives the users
// and sessions it names, and records sign-in attempts for unknown users too).
internal sealed class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("SecurityEvents", IdentityTables.Schema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.OccurredAtUtc).IsUtcTimestamp();
        builder.Property(x => x.IpAddress).HasMaxLength(45); // IPv6 text form
        builder.Property(x => x.CorrelationId).HasMaxLength(64);
        builder.Property(x => x.Details); // nvarchar(max): small JSON objects, lists of ids at most

        // Newest first, overall and per user (as actor or as target).
        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => new { x.TargetUserId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.ActorUserId, x.OccurredAtUtc });
    }
}
