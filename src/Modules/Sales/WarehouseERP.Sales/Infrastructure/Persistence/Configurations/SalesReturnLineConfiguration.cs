using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// V1 restocks only goods fit for resale (condition 1); the split between uninvoiced and invoiced parts always adds up to
// the line once approved. Foreign keys to Catalog, Organization and Inventory's stock document lines live in Persistence.
internal sealed class SalesReturnLineConfiguration : IEntityTypeConfiguration<SalesReturnLine>
{
    public void Configure(EntityTypeBuilder<SalesReturnLine> b)
    {
        b.ToTable("SalesReturnLines", "sales", t =>
        {
            t.HasCheckConstraint("CK_SalesReturnLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
            t.HasCheckConstraint("CK_SalesReturnLines_Condition", "[Condition] = 1");
            t.HasCheckConstraint("CK_SalesReturnLines_Split",
                "[UninvoicedQuantity] >= 0 AND [InvoicedQuantity] >= 0 AND [CreditNetAmount] >= 0 AND [CreditTaxAmount] >= 0"
                + " AND ([StockLineId] IS NULL OR [UninvoicedQuantity] + [InvoicedQuantity] = [Quantity])");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.ConversionFactor).HasPrecision(18, 8);
        b.Property(x => x.Quantity).HasPrecision(18, 4);
        b.Property(x => x.Condition).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.CostAmount).HasPrecision(19, 4);
        b.Property(x => x.UninvoicedQuantity).HasPrecision(18, 4);
        b.Property(x => x.InvoicedQuantity).HasPrecision(18, 4);
        b.Property(x => x.CreditNetAmount).HasPrecision(19, 4);
        b.Property(x => x.CreditTaxAmount).HasPrecision(19, 4);
        b.HasOne<SalesDeliveryLine>().WithMany().HasForeignKey(x => x.DeliveryLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SalesOrderLine>().WithMany().HasForeignKey(x => x.OrderLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ReturnId, x.LineNumber }).IsUnique();
        b.HasIndex(x => x.DeliveryLineId);
        b.HasIndex(x => x.OrderLineId);
        b.HasIndex(x => x.StockLineId).IsUnique().HasFilter("[StockLineId] IS NOT NULL");
    }
}
