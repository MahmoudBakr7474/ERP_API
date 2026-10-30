using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

// The open items a voucher draft plans to settle; they become allocations when it posts.
internal sealed class VoucherAllocationConfiguration : IEntityTypeConfiguration<VoucherAllocation>
{
    public void Configure(EntityTypeBuilder<VoucherAllocation> b)
    {
        b.ToTable("VoucherAllocations", "accounting", t => t.HasCheckConstraint("CK_VoucherAllocations_Amount", "[Amount] > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Amount).HasPrecision(19, 4);
        b.HasOne<OpenItem>().WithMany().HasForeignKey(x => x.OpenItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.JournalEntryId, x.OpenItemId }).IsUnique();
        b.HasIndex(x => x.OpenItemId);
    }
}
