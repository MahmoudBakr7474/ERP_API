using Microsoft.EntityFrameworkCore;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Application;

// Commands that keep cross-row invariants. Production API must also validate authorization,
// input and uniqueness errors. The DbContext is the shared ErpDbContext; this module touches
// only its own tables through Set<T>().
public sealed class WarehouseCommands(DbContext db)
{
    public async Task<Guid> CreateWarehouseAsync(
        Guid branchId, string code, string nameAr, string? nameEn,
        WarehouseType type, bool allowsSelling, Guid? actorId, CancellationToken ct)
    {
        if (type == WarehouseType.Transit && allowsSelling)
            throw new ArgumentException("Transit warehouses may never allow selling.");
        if (!await db.Set<Branch>().AnyAsync(x => x.Id == branchId && x.IsActive, ct))
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
        db.Set<Warehouse>().Add(warehouse);
        await db.SaveChangesAsync(ct);
        return warehouse.Id;
    }
}
