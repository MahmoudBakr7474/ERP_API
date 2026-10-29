using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Pos.Application.Contracts.Items;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Domain;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Items;

// What a till shows for a product unit: Sales' list price and the product's tax classification, the price with VAT for
// display, and Inventory's unreserved stock in the terminal's selling location. A unit that can't be sold says why with the
// code checkout would refuse it with; stock is only a hint (checkout checks it again under the warehouse lock).
public sealed class PosItems(ISalesPricing pricing, IStockAvailabilityLookup stock, PosReferences references)
{
    public async Task<Result<IReadOnlyList<PosItemResponse>>> DescribeAsync(PosTerminal terminal, IReadOnlyList<PosUnit> units, CancellationToken ct)
    {
        if (units.Count == 0)
            return Result.Success<IReadOnlyList<PosItemResponse>>([]);
        var currency = await references.CurrencyAsync(ct);
        if (currency.IsFailure)
            return currency.Error;
        var prices = await pricing.FindUnitPricesAsync(units.Select(u => new SalesProductUnit(u.Product.Id, u.Unit.Id)).ToList(), ct);
        var available = (await stock.GetAsync(terminal.WarehouseId, terminal.SellingLocationId,
                units.Select(u => u.Product.Id).Distinct().ToList(), ct))
            .ToDictionary(a => a.ProductId);

        var items = new List<PosItemResponse>(units.Count);
        for (var i = 0; i < units.Count; i++)
        {
            var (product, unit) = (units[i].Product, units[i].Unit);
            var price = prices[i];
            var sellable = available.TryGetValue(product.Id, out var stockOf) ? stockOf.Sellable : 0m;
            var problem = product.Type != ProductType.StockItem ? "Pos.Sale.ProductNotStockItem"
                : product.TrackingMode != TrackingMode.None ? "Pos.Sale.TrackingNotSupported"
                : price.UnitPrice is null ? "Sales.Line.PriceMissing"
                : price.TaxTreatment is null ? "Sales.ProductTax.NotClassified"
                : sellable <= 0 ? "Inventory.Stock.Insufficient"
                : null;
            items.Add(new PosItemResponse(
                product.Id, product.Sku, product.NameAr, unit.Id, unit.UnitId, unit.UnitCode, unit.UnitNameAr, unit.ConversionFactor,
                unit.IsBase, unit.UnitAllowsFraction, currency.Value.Code, price.UnitPrice,
                price.UnitPrice is { } listed && price.TaxTreatment is not null
                    ? PosReferences.WithTax(listed, price.TaxRate, currency.Value.Decimals)
                    : null,
                price.TaxTreatment is { } treatment ? PosReferences.TreatmentOf(treatment) : null, price.TaxRate, sellable,
                decimal.Round(sellable / unit.ConversionFactor, Money.QuantityScale, MidpointRounding.ToZero), problem is null, problem));
        }
        return Result.Success<IReadOnlyList<PosItemResponse>>(items);
    }
}
