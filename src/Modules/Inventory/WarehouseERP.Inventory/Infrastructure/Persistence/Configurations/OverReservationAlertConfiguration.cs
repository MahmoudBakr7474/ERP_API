using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

internal sealed class OverReservationAlertConfiguration : IEntityTypeConfiguration<OverReservationAlert>
{
    public void Configure(EntityTypeBuilder<OverReservationAlert> b)
    {
        b.ToTable("OverReservationAlerts", "inventory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.OnHandSellable).HasPrecision(18, 4);
        b.Property(x => x.ReservedQuantity).HasPrecision(18, 4);
        b.Property(x => x.DetectedAtUtc).IsUtcTimestamp();
        b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.WarehouseId, x.ProductId });
        b.HasIndex(x => x.DocumentId);
    }
}
