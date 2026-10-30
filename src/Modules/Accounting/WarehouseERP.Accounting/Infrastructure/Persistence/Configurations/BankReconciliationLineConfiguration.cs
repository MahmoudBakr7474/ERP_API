using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// A ledger line is ticked by one reconciliation at most (a cancelled one releases its lines).
internal sealed class BankReconciliationLineConfiguration : IEntityTypeConfiguration<BankReconciliationLine>
{
    public void Configure(EntityTypeBuilder<BankReconciliationLine> b)
    {
        b.ToTable("BankReconciliationLines", "accounting");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Amount).HasPrecision(19, 4);
        b.HasOne<JournalLine>().WithMany().HasForeignKey(x => x.JournalLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.JournalLineId, "UX_BankReconciliationLines_Line").IsUnique();
        b.HasIndex(x => x.BankReconciliationId);
    }
}
