using FluentValidation;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Prices;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Sales.Application.Contracts.Documents;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Shared;

// How quotation and order lines take their prices, as a snapshot: the price list's price for the product unit unless the
// line names another, which only a user with sales.prices.manage may do (a manual price, also where the list has none). No
// line is ever priced at nothing: a unit without a list price and without an authorized manual price is refused. VAT is
// standard-rated at the company's default rate unless the line says otherwise; zero-rated and exempt lines take no rate.
public sealed class LinePricing(
    SalesReferences references, ISalesPriceRepository prices, ISalesSettingsRepository settings, ICurrentUser currentUser)
{
    public static void Rules(InlineValidator<SalesLineRequest> line)
    {
        line.RuleLevelCascadeMode = CascadeMode.Stop;
        line.RuleFor(x => x.ProductId).NotEmpty();
        line.RuleFor(x => x.ProductUnitId).NotEmpty();
        line.RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(Amounts.MaxQuantity)
            .Must(q => Amounts.FitsScale(q, Amounts.QuantityScale)).WithMessage("'Quantity' may have at most 4 decimals.");
        line.RuleFor(x => x.UnitPrice!.Value).GreaterThan(0).LessThanOrEqualTo(Amounts.MaxUnitPrice)
            .Must(p => Amounts.FitsScale(p, Amounts.PriceScale)).WithMessage("'Unit Price' may have at most 8 decimals.")
            .OverridePropertyName("UnitPrice").When(x => x.UnitPrice.HasValue);
        line.RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100)
            .Must(p => Amounts.FitsScale(p, Amounts.PercentScale)).WithMessage("'Discount Percent' may have at most 4 decimals.");
        line.RuleFor(x => x.TaxTreatment!.Value).IsInEnum().OverridePropertyName("TaxTreatment").When(x => x.TaxTreatment.HasValue);
        line.RuleFor(x => x.TaxRate!.Value).InclusiveBetween(0, 100)
            .Must(p => Amounts.FitsScale(p, Amounts.PercentScale)).WithMessage("'Tax Rate' may have at most 4 decimals.")
            .OverridePropertyName("TaxRate").When(x => x.TaxRate.HasValue);
    }

    public async Task<Result<IReadOnlyList<SalesLineInput>>> PriceAsync(IReadOnlyList<SalesLineRequest> lines, CancellationToken ct)
    {
        var units = await references.ResolveUnitsAsync(lines.Select(l => new ProductUnitRef(l.ProductId, l.ProductUnitId)).ToList(), ct);
        if (units.IsFailure)
            return units.Error;
        var listed = await prices.FindActivePricesAsync(lines.Select(l => l.ProductUnitId).Distinct().ToList(), ct);
        var defaultVatRate = (await settings.GetAsync(ct)).DefaultVatRate;
        var mayPrice = currentUser.HasPermission(SalesPermissions.PricesManage);

        var priced = new List<SalesLineInput>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var number = i + 1;
            decimal? listPrice = listed.TryGetValue(line.ProductUnitId, out var found) ? found : null;
            decimal unitPrice;
            PriceSource source;
            if (line.UnitPrice is not { } asked)
            {
                if (listPrice is not { } fromList)
                    return PriceErrors.PriceMissing(number);
                (unitPrice, source) = (fromList, PriceSource.PriceList);
            }
            else if (asked == listPrice)
                (unitPrice, source) = (asked, PriceSource.PriceList);
            else if (!mayPrice)
                return PriceErrors.ManualPriceNotAllowed(number);
            else
                (unitPrice, source) = (asked, PriceSource.Manual);

            var treatment = line.TaxTreatment ?? SalesTaxTreatment.Standard;
            decimal taxRate;
            if (treatment == SalesTaxTreatment.Standard)
                taxRate = line.TaxRate ?? defaultVatRate;
            else if (line.TaxRate is null or 0)
                taxRate = 0;
            else
                return PriceErrors.TaxRateNotAllowed(number);

            priced.Add(new SalesLineInput(
                units.Value[i], line.Quantity, new LinePrice(unitPrice, line.DiscountPercent, treatment, taxRate), source, listPrice));
        }
        return priced;
    }
}
