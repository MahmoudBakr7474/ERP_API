using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Units;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Infrastructure.Persistence;

internal sealed class UnitRepository(DbContext db) : IUnitRepository
{
    // Unique indexes whose violation is an expected business conflict. Names come from Migration 01.
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["IX_Units_Code"] = UnitErrors.CodeTaken,
    };

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        db.Set<Unit>().AnyAsync(x => x.Code == code, ct);

    public Task<Result> AddAsync(Unit unit, CancellationToken ct)
    {
        db.Set<Unit>().Add(unit);
        return DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
    }

    public async Task<Unit?> GetForUpdateAsync(Guid unitId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var unit = await db.Set<Unit>().SingleOrDefaultAsync(x => x.Id == unitId, ct);
        if (unit is not null && expectedRowVersion is not null)
            // The UPDATE's WHERE uses the original value, so this makes it compare with the caller's version.
            db.Entry(unit).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return unit;
    }

    public async Task<bool> IsUsedByActiveProductsAsync(Guid unitId, CancellationToken ct) =>
        await db.Set<Product>().AnyAsync(x => x.BaseUnitId == unitId && x.IsActive, ct)
        || await db.Set<ProductUnit>().AnyAsync(x => x.UnitId == unitId && x.IsActive && x.Product.IsActive, ct);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
}
