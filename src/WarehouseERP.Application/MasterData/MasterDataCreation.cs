using Microsoft.EntityFrameworkCore;
using WarehouseERP.Domain.Catalog;
using WarehouseERP.Domain.Organization;
using WarehouseERP.Persistence;

namespace WarehouseERP.Application.MasterData;

// Minimal command examples for critical cross-row invariants.
// Production API must also validate authorization, input and uniqueness errors.
public sealed class MasterDataCreation(ErpDbContext db)
{
    public async Task<Guid> CreateWarehouseAsync(
        Guid branchId, string code, string nameAr, string? nameEn,
        WarehouseType type, bool allowsSelling, Guid? actorId, CancellationToken ct)
    {
        if (type == WarehouseType.Transit && allowsSelling)
            throw new ArgumentException("Transit warehouses may never allow selling.");
        if (!await db.Branches.AnyAsync(x => x.Id == branchId && x.IsActive, ct))
            throw new InvalidOperationException("Branch does not exist or is inactive.");
        var warehouse = new Warehouse
        {
            BranchId = branchId, Code = code.Trim(), NameAr = nameAr.Trim(),
            NameEn = nameEn?.Trim(), WarehouseType = type,
            AllowsSelling = allowsSelling, CreatedByUserId = actorId
        };
        warehouse.Locations.Add(new WarehouseLocation
        {
            Code = "DEFAULT", NameAr = "الموقع الافتراضي", NameEn = "Default location",
            IsDefault = true, CreatedByUserId = actorId
        });
        // SaveChanges uses one transaction, preserving Warehouse + DEFAULT location.
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(ct);
        return warehouse.Id;
    }

    public async Task<Guid> CreateProductAsync(
        string sku, string nameAr, Guid baseUnitId, Guid? categoryId,
        ProductType type, TrackingMode tracking, Guid? actorId, CancellationToken ct)
    {
        if (type != ProductType.StockItem && tracking != TrackingMode.None)
            throw new ArgumentException("Only stock items can use batch/serial tracking.");
        if (!await db.Units.AnyAsync(u => u.Id == baseUnitId && u.IsActive, ct))
            throw new InvalidOperationException("Base unit does not exist or is inactive.");
        var product = new Product
        {
            SKU = sku.Trim(), NameAr = nameAr.Trim(), CategoryId = categoryId,
            BaseUnitId = baseUnitId, ProductType = type,
            TrackingMode = tracking, CreatedByUserId = actorId
        };
        product.Units.Add(new ProductUnit
        {
            UnitId = baseUnitId, ConversionFactor = 1m, CreatedByUserId = actorId
        });
        // Product.BaseUnitId alone stores base-unit truth. This row is still required.
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return product.Id;
    }
}
