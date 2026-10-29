using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Shared;

// What sales documents refer to, looked up through Organization's and Catalog's contracts only: the company (its base
// currency and time zone), active physical warehouses that allow selling and their locations, active untracked stock
// items with their active product units, and customers. Inventory checks stock-related facts again when it reserves,
// delivers or takes goods back.
public sealed class SalesReferences(
    ICompanyDirectory companies, IWarehouseDirectory warehouses, IWarehouseLocationDirectory locations, IProductCatalog products,
    ICustomerRepository customers, TimeProvider clock)
{
    public async Task<Result<CompanyInfo>> CompanyAsync(CancellationToken ct) =>
        await companies.GetAsync(ct) is { } company ? company : ReferenceErrors.CompanySetupRequired;

    /// <summary>Today in the company's time zone, the business date documents are dated and compared with.</summary>
    public DateOnly Today(CompanyInfo company) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(clock.GetUtcNow().UtcDateTime,
            TimeZoneInfo.FindSystemTimeZoneById(company.TimeZoneId)));

    /// <summary>Today in the company's time zone, or why it can't be told.</summary>
    public async Task<Result<DateOnly>> TodayAsync(CancellationToken ct)
    {
        var company = await CompanyAsync(ct);
        return company.IsFailure ? company.Error : Today(company.Value);
    }

    /// <summary>The warehouse when Sales may sell from it: it exists, is active, is physical and allows selling.</summary>
    public async Task<Result<WarehouseInfo>> SellingWarehouseAsync(Guid warehouseId, CancellationToken ct)
    {
        var warehouse = await warehouses.FindAsync(warehouseId, ct);
        if (warehouse is null)
            return ReferenceErrors.WarehouseNotFound;
        if (!warehouse.IsActive)
            return ReferenceErrors.WarehouseNotActive;
        if (warehouse.Type != WarehouseType.Physical || !warehouse.AllowsSelling)
            return ReferenceErrors.WarehouseNotSelling;
        return warehouse;
    }

    /// <summary>A customer, active or not.</summary>
    public async Task<Result<CustomerState>> CustomerAsync(Guid customerId, CancellationToken ct) =>
        await customers.FindStateAsync(customerId, ct) is { } customer ? customer : CustomerErrors.NotFound;

    /// <summary>An active customer, which new quotations and orders need.</summary>
    public async Task<Result<CustomerState>> ActiveCustomerAsync(Guid customerId, CancellationToken ct)
    {
        var customer = await CustomerAsync(customerId, ct);
        if (customer.IsFailure)
            return customer.Error;
        return customer.Value.IsActive ? customer.Value : CustomerErrors.NotActive;
    }

    /// <summary>
    /// Each new line's product unit facts, when the product is an active stock item without batch or serial tracking (V1
    /// sells nothing else) and the unit is an active unit of it. Line numbers count from 1 in the order given.
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
            resolved.Add(Facts(unit));
        }
        return resolved;
    }

    /// <summary>
    /// The facts of product units already on a confirmed document (a delivery, invoice or return of its lines), active or
    /// not: goods sold before a unit was retired are still delivered and billed in it.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, ProductUnitFacts>> UnitFactsAsync(IReadOnlyCollection<Guid> productUnitIds, CancellationToken ct) =>
        (await products.FindUnitsAsync(productUnitIds.Distinct().ToList(), ct)).ToDictionary(u => u.Id, Facts);

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

    private static ProductUnitFacts Facts(ProductUnitInfo unit) =>
        new(unit.ProductId, unit.Id, unit.UnitId, unit.ConversionFactor, unit.UnitAllowsFraction, unit.BaseUnitAllowsFraction);
}
