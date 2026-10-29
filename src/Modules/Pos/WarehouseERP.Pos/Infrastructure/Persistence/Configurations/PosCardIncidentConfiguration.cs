using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Infrastructure.Persistence.Configurations;

// One incident per checkout key; a resolved one says who settled it and how. Foreign keys to Organization live in
// Persistence.
internal sealed class PosCardIncidentConfiguration : IEntityTypeConfiguration<PosCardIncident>
{
    public void Configure(EntityTypeBuilder<PosCardIncident> b)
    {
        b.ToTable("PosCardIncidents", "pos", t =>
        {
            t.HasCheckConstraint("CK_PosCardIncidents_Status", "[Status] BETWEEN 1 AND 3");
            t.HasCheckConstraint("CK_PosCardIncidents_Amount", "[CardAmount] > 0 AND [Attempts] >= 1");
            t.HasCheckConstraint("CK_PosCardIncidents_Resolved",
                "([Status] = 1 AND [ResolvedAtUtc] IS NULL) OR ([Status] = 2 AND [SaleId] IS NOT NULL AND [ResolvedAtUtc] IS NOT NULL)"
                + " OR ([Status] = 3 AND [ResolvedAtUtc] IS NOT NULL AND [ResolutionNote] IS NOT NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.CardAmount).HasPrecision(19, 4);
        b.Property(x => x.References).HasMaxLength(PosCardIncident.MaxReferencesLength);
        b.Property(x => x.ErrorCode).HasMaxLength(100).IsUnicode(false);
        b.Property(x => x.ErrorMessage).HasMaxLength(PosCardIncident.MaxErrorLength);
        b.Property(x => x.ResolutionNote).HasMaxLength(PosCardIncident.MaxNoteLength);
        b.Property(x => x.OccurredAtUtc).IsUtcTimestamp();
        b.Property(x => x.LastAttemptAtUtc).IsUtcTimestamp();
        b.Property(x => x.ResolvedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne<PosTerminal>().WithMany().HasForeignKey(x => x.TerminalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PosShift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PosSale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.IdempotencyKey, "UX_PosCardIncidents_Key").IsUnique();
        b.HasIndex(x => new { x.WarehouseId, x.Status });
        b.HasIndex(x => x.ShiftId);
    }
}
