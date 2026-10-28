using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Shared;

// What purchasing documents refer to, looked up through Organization's and Catalog's contracts only: the company (its
// base currency and time zone), active physical warehouses and their locations, active untracked stock items with their
// active product units, and active suppliers. Inventory checks stock-related facts again when it posts.
public sealed class PurchasingReferences(
    ICompanyDirectory companies, IWarehouseDirectory warehouses, IWarehouseLocationDirectory locations, IProductCatalog products,
    ISupplierRepository suppliers, TimeProvider clock)
{
    public async Task<Result<CompanyInfo>> CompanyAsync(CancellationToken ct) =>
        await companies.GetAsync(ct) is { } company ? company : ReferenceErrors.CompanySetupRequired;

    /// <summary>Today in the company's time zone, the business date invoices are compared with.</summary>
    public DateOnly Today(CompanyInfo company) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(clock.GetUtcNow().UtcDateTime,
            TimeZoneInfo.FindSystemTimeZoneById(company.TimeZoneId)));

    /// <summary>The warehouse when purchasing may use it: it exists, is active and is physical.</summary>
    public async Task<Result<WarehouseInfo>> UsableWarehouseAsync(Guid warehouseId, CancellationToken ct)
    {
        var warehouse = await warehouses.FindAsync(warehouseId, ct);
        if (warehouse is null)
            return ReferenceErrors.WarehouseNotFound;
        if (!warehouse.IsActive)
            return ReferenceErrors.WarehouseNotActive;
        if (warehouse.Type == WarehouseType.Transit)
            return ReferenceErrors.WarehouseIsTransit;
        return warehouse;
    }

    /// <summary>An active supplier, which new orders and their approval need.</summary>
    public async Task<Result<SupplierState>> ActiveSupplierAsync(Guid supplierId, CancellationToken ct)
    {
        var supplier = await suppliers.FindStateAsync(supplierId, ct);
        if (supplier is null)
            return SupplierErrors.NotFound;
        return supplier.IsActive ? supplier : SupplierErrors.NotActive;
    }

    /// <summary>
    /// Each line's product unit facts, when the product is an active stock item without batch or serial tracking (V1
    /// supports nothing else) and the unit is an active unit of it. Line numbers count from 1 in the order given.
    /// </summary>
    public async Task<Result<IReadOnlyList<ProductUnitFacts>>> ResolveUnitsAsync(IReadOnlyList<ProductUnitRef> lines, CancellationToken ct)
    {
        var productInfos = (await products.FindManyAsync(lines.Select(l => l.ProductId).Distinct().ToList(), ct)).ToDictionary(p => p.Id);
        var units = (await products.FindUnitsAsync(lines.Select(l => l.ProductUnitId).Distinct().ToList(), ct)).ToDictionary(u => u.Id);
        var resolved = new List<ProductUnitFacts>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var number = i + 1;
            var product = productInfos.GetValueOrDefault(lines[i].ProductId);
            if (product is null)
                return ReferenceErrors.ProductNotFound(number);
            if (product.Type != ProductType.StockItem)
                return ReferenceErrors.ProductNotStockItem(number);
            if (product.TrackingMode != TrackingMode.None)
                return ReferenceErrors.TrackingNotSupported(number);
            if (!product.IsActive)
                return ReferenceErrors.ProductNotActive(number);
            var unit = units.GetValueOrDefault(lines[i].ProductUnitId);
            if (unit is null || unit.ProductId != product.Id)
                return ReferenceErrors.ProductUnitNotFound(number);
            if (!unit.IsActive)
                return ReferenceErrors.ProductUnitNotActive(number);
            resolved.Add(new ProductUnitFacts(
                product.Id, unit.Id, unit.UnitId, unit.ConversionFactor, unit.UnitAllowsFraction, unit.BaseUnitAllowsFraction));
        }
        return resolved;
    }

    /// <summary>
    /// The location of each line: the one named, when it is an active location of the warehouse, or the warehouse's
    /// DEFAULT location.
    /// </summary>
    public async Task<Result<IReadOnlyList<Guid>>> ResolveLocationsAsync(Guid warehouseId, IReadOnlyList<Guid?> named, CancellationToken ct)
    {
        var places = (await locations.FindManyAsync(named.OfType<Guid>().Distinct().ToList(), ct)).ToDictionary(l => l.Id);
        var fallback = named.Any(n => n is null) ? await locations.FindDefaultAsync(warehouseId, ct) : null;
        var resolved = new List<Guid>(named.Count);
        for (var i = 0; i < named.Count; i++)
        {
            var location = named[i] is { } id ? places.GetValueOrDefault(id) : fallback;
            if (location is null || location.WarehouseId != warehouseId)
                return ReferenceErrors.LocationNotFound(i + 1);
            if (!location.IsActive)
                return ReferenceErrors.LocationNotActive(i + 1);
            resolved.Add(location.Id);
        }
        return resolved;
    }
}
