using WarehouseERP.Sales.Application.Abstractions.Interfaces.Prices;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.ProductTaxes;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Prices;

// The price list and the line amount rules of sales documents, for a module that sells without choosing prices or taxes
// per line (POS): the list price of the product unit, the product's tax classification (its own rate, or the settings'
// default VAT for standard-rated products), the line discount, and LineAmounts' rounding. Nothing here is computed twice:
// sales documents and tills share LineAmounts.Calculate.
public sealed class SalesPricing(
    SalesReferences references, ISalesPriceRepository prices, IProductTaxRepository taxes, ISalesSettingsRepository settings)
    : ISalesPricing
{
    public async Task<IReadOnlyList<SalesUnitPrice>> FindUnitPricesAsync(IReadOnlyCollection<SalesProductUnit> units, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(units);
        if (units.Count == 0)
            return [];
        var listed = await prices.FindActivePricesAsync(units.Select(u => u.ProductUnitId).Distinct().ToList(), ct);
        var classified = await taxes.FindActiveAsync(units.Select(u => u.ProductId).Distinct().ToList(), ct);
        var defaultVatRate = (await settings.GetAsync(ct)).DefaultVatRate;
        return units.Select(u =>
            {
                var tax = classified.GetValueOrDefault(u.ProductId);
                return new SalesUnitPrice(u.ProductId, u.ProductUnitId, listed.TryGetValue(u.ProductUnitId, out var price) ? price : null,
                    tax?.TaxTreatment, tax?.RateWith(defaultVatRate) ?? 0m);
            })
            .ToList();
    }

    public async Task<SalesPricingOutcome> PriceLinesAsync(IReadOnlyList<SalesPricingLine> lines, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return Refused(company.Error);
        var decimals = Amounts.CurrencyDecimals(company.Value.BaseCurrencyCode);
        var units = await FindUnitPricesAsync(lines.Select(l => new SalesProductUnit(l.ProductId, l.ProductUnitId)).ToList(), ct);

        var priced = new List<SalesPricedLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var number = i + 1;
            var line = lines[i];
            if (units[i].UnitPrice is not { } unitPrice)
                return Refused(PriceErrors.PriceMissing(number));
            if (units[i].TaxTreatment is not { } treatment)
                return Refused(ProductTaxErrors.NotClassified(number));
            if (line.Quantity <= 0 || line.Quantity > Amounts.MaxQuantity || !Amounts.FitsScale(line.Quantity, Amounts.QuantityScale))
                return Refused(SalesLineDomainErrors.QuantityInvalid(number));
            var price = new LinePrice(unitPrice, line.DiscountPercent, treatment, units[i].TaxRate);
            var amounts = LineAmounts.Calculate(line.Quantity, price, decimals, number);
            if (amounts.IsFailure)
                return Refused(amounts.Error);
            priced.Add(new SalesPricedLine(
                unitPrice, line.DiscountPercent, treatment, units[i].TaxRate, amounts.Value.Gross, amounts.Value.Discount,
                amounts.Value.Net, amounts.Value.Tax, amounts.Value.Total, amounts.Value.NetUnitPrice));
        }
        return SalesPricingOutcome.Priced(company.Value.BaseCurrencyCode, decimals, priced);
    }

    private static SalesPricingOutcome Refused(Error error) => SalesPricingOutcome.Refused(error.Code, error.Message);
}
