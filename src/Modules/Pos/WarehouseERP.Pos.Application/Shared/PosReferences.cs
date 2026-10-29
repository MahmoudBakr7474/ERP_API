using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Domain;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Shared;

// What tills refer to, through other modules' contracts only: the company (its time zone and calendar), the base currency
// and its decimals (Sales' amount rule), physical warehouses that allow selling and their locations, and active untracked
// stock items with their active units. Inventory checks stock facts again when it posts.
public sealed class PosReferences(
    ICompanyDirectory companies, IWarehouseDirectory warehouses, IWarehouseLocationDirectory locations, IProductCatalog products,
    ISalesPricing pricing, TimeProvider clock)
{
    public async Task<Result<CompanyInfo>> CompanyAsync(CancellationToken ct) =>
        await companies.GetAsync(ct) is { } company ? company : ReferenceErrors.CompanySetupRequired;

    /// <summary>The business date of a moment in the company's time zone.</summary>
    public static DateOnly DateOf(DateTime utc, CompanyInfo company) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(company.TimeZoneId)));

    /// <summary>Today in the company's time zone.</summary>
    public DateOnly Today(CompanyInfo company) => DateOf(clock.GetUtcNow().UtcDateTime, company);

    /// <summary>The UTC moment a business date starts in the company's time zone.</summary>
    public static DateTime StartOf(DateOnly date, CompanyInfo company) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), TimeZoneInfo.FindSystemTimeZoneById(company.TimeZoneId));

    /// <summary>The base currency and the decimals amounts are rounded to, as Sales prices them.</summary>
    public async Task<Result<(string Code, int Decimals)>> CurrencyAsync(CancellationToken ct)
    {
        var outcome = await pricing.PriceLinesAsync([], ct);
        return outcome.Succeeded ? (outcome.CurrencyCode!, outcome.CurrencyDecimals) : PricingErrors.Refused(outcome);
    }

    /// <summary>The warehouse when a till may sell from it: it exists, is active, is physical and allows selling.</summary>
    public async Task<Result<WarehouseInfo>> SellingWarehouseAsync(Guid warehouseId, CancellationToken ct)
    {
        var warehouse = await warehouses.FindAsync(warehouseId, ct);
        if (warehouse is null)
            return TerminalErrors.WarehouseNotFound;
        if (!warehouse.IsActive)
            return TerminalErrors.WarehouseNotActive;
        if (warehouse.Type != WarehouseType.Physical || !warehouse.AllowsSelling)
            return TerminalErrors.WarehouseNotSelling;
        return warehouse;
    }

    /// <summary>The named location when it is an active location of the warehouse, or the warehouse's DEFAULT location.</summary>
    public async Task<Result<Guid>> SellingLocationAsync(Guid warehouseId, Guid? locationId, CancellationToken ct)
    {
        var location = locationId is { } id
            ? (await locations.FindManyAsync([id], ct)).SingleOrDefault()
            : await locations.FindDefaultAsync(warehouseId, ct);
        if (location is null || location.WarehouseId != warehouseId)
            return TerminalErrors.LocationNotFound;
        if (!location.IsActive)
            return TerminalErrors.LocationNotActive;
        return location.Id;
    }

    /// <summary>
    /// Each cart line's product and unit, when the product is an active untracked stock item and the unit an active unit of
    /// it. Line numbers count from 1 in the order given.
    /// </summary>
    public async Task<Result<IReadOnlyList<PosUnit>>> ResolveUnitsAsync(IReadOnlyList<(Guid ProductId, Guid ProductUnitId)> lines, CancellationToken ct)
    {
        var productInfos = (await products.FindManyAsync(lines.Select(l => l.ProductId).Distinct().ToList(), ct)).ToDictionary(p => p.Id);
        var units = (await products.FindUnitsAsync(lines.Select(l => l.ProductUnitId).Distinct().ToList(), ct)).ToDictionary(u => u.Id);
        var resolved = new List<PosUnit>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var number = i + 1;
            var product = productInfos.GetValueOrDefault(lines[i].ProductId);
            if (product is null)
                return SaleErrors.ProductNotFound(number);
            if (product.Type != ProductType.StockItem)
                return SaleErrors.ProductNotStockItem(number);
            if (product.TrackingMode != TrackingMode.None)
                return SaleErrors.TrackingNotSupported(number);
            if (!product.IsActive)
                return SaleErrors.ProductNotActive(number);
            var unit = units.GetValueOrDefault(lines[i].ProductUnitId);
            if (unit is null || unit.ProductId != product.Id)
                return SaleErrors.ProductUnitNotFound(number);
            if (!unit.IsActive)
                return SaleErrors.ProductUnitNotActive(number);
            resolved.Add(new PosUnit(product, unit));
        }
        return resolved;
    }

    /// <summary>A sold line's VAT treatment as POS records it.</summary>
    public static PosTaxTreatment TreatmentOf(SalesTaxTreatment treatment) => treatment switch
    {
        SalesTaxTreatment.Standard => PosTaxTreatment.Standard,
        SalesTaxTreatment.ZeroRated => PosTaxTreatment.ZeroRated,
        SalesTaxTreatment.Exempt => PosTaxTreatment.Exempt,
        _ => throw new ArgumentOutOfRangeException(nameof(treatment), treatment, "Unknown tax treatment."),
    };

    /// <summary>A tax-exclusive unit price with its VAT, rounded to the currency, for the cashier's and customer's eyes only.</summary>
    public static decimal WithTax(decimal unitPrice, decimal taxRate, int currencyDecimals) =>
        Money.Round(unitPrice * (100m + taxRate) / 100m, currencyDecimals);
}
