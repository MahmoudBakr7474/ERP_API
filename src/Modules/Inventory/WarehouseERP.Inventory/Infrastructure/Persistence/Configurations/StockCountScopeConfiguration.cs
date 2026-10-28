using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence.Configurations;

// The freeze is these rows, nothing else: at most one active whole-warehouse scope per warehouse and one active scope
// per location. A whole-warehouse scope and a location scope of the same warehouse are kept apart by the count's start,
// which holds the warehouse's exclusive lock.
internal sealed class StockCountScopeConfiguration : IEntityTypeConfiguration<StockCountScope>
{
    public void Configure(EntityTypeBuilder<StockCountScope> b)
    {
        b.ToTable("StockCountScopes", "inventory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.HasIndex(x => x.WarehouseId, "UX_StockCountScopes_ActiveWarehouse")
            .IsUnique().HasFilter("[IsActive] = 1 AND [LocationId] IS NULL");
        b.HasIndex(x => x.LocationId, "UX_StockCountScopes_ActiveLocation")
            .IsUnique().HasFilter("[IsActive] = 1 AND [LocationId] IS NOT NULL");
        b.HasIndex(x => new { x.WarehouseId, x.IsActive });
        b.HasIndex(x => x.CountId);
    }
}
