using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// A posted delivery always has its number and its stock document; one delivery per stock document (and the stock
// document names the delivery back, unique too), so a delivery can't be posted to stock twice. Foreign keys to
// Organization and Inventory live in Persistence.
internal sealed class SalesDeliveryConfiguration : IEntityTypeConfiguration<SalesDelivery>
{
    public void Configure(EntityTypeBuilder<SalesDelivery> b)
    {
        b.ToTable("SalesDeliveries", "sales", t =>
        {
            t.HasCheckConstraint("CK_SalesDeliveries_Status", "[Status] BETWEEN 1 AND 3");
            t.HasCheckConstraint("CK_SalesDeliveries_Posted",
                "([Status] = 2 AND [Number] IS NOT NULL AND [StockDocumentId] IS NOT NULL AND [PostedAtUtc] IS NOT NULL)"
                + " OR ([Status] <> 2 AND [Number] IS NULL AND [StockDocumentId] IS NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.DraftNumber).UseIdentityColumn();
        b.Property(x => x.Number).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.Status).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.StockDocumentNumber).HasMaxLength(60).IsUnicode(false);
        b.Property(x => x.CostAmount).HasPrecision(19, 4);
        b.Property(x => x.PostedAtUtc).IsUtcTimestamp();
        b.Property(x => x.CreatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.UpdatedAtUtc).IsUtcTimestamp();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.DeliveryId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<SalesOrder>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.DraftNumber).IsUnique();
        b.HasIndex(x => x.Number).IsUnique().HasFilter("[Number] IS NOT NULL");
        b.HasIndex(x => new { x.BranchId, x.FiscalYear, x.SequenceNumber }).IsUnique().HasFilter("[SequenceNumber] IS NOT NULL");
        b.HasIndex(x => x.StockDocumentId, "UX_SalesDeliveries_StockDocument").IsUnique().HasFilter("[StockDocumentId] IS NOT NULL");
        b.HasIndex(x => x.OrderId);
        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => new { x.WarehouseId, x.Status });
        b.HasIndex(x => x.CreatedAtUtc);
    }
}
