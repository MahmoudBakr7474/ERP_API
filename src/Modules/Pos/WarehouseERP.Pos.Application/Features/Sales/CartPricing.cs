using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Pos.Application.Contracts.Sales;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Domain;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Sales;

// How a cart is priced, for a quote and for the checkout: each line an active unit of an active untracked stock item, in a
// quantity that converts exactly to base units; a discount only with pos.discounts.apply and never above the settings'
// maximum (no manager override); then Sales prices every line with its own rules (list price, product tax classification,
// rounding). Nothing manual: a unit without a price or a product without a tax classification is refused.
public sealed class CartPricing(PosReferences references, ISalesPricing pricing, IPosSettingsRepository settings, ICurrentUser currentUser)
{
    public async Task<Result<PricedCart>> PriceAsync(IReadOnlyList<PosCartLineRequest> lines, CancellationToken ct)
    {
        if (lines.Count == 0)
            return PosSaleDomainErrors.NoLines;
        if (lines.Count > PosSale.MaxLines)
            return PosSaleDomainErrors.TooManyLines;
        var units = await references.ResolveUnitsAsync(lines.Select(l => (l.ProductId, l.ProductUnitId)).ToList(), ct);
        if (units.IsFailure)
            return units.Error;
        var maxDiscount = (await settings.GetAsync(ct)).MaxLineDiscountPercent;
        var mayDiscount = currentUser.HasPermission(PosPermissions.DiscountsApply);
        for (var i = 0; i < lines.Count; i++)
        {
            var quantity = CheckQuantity(lines[i].Quantity, units.Value[i], i + 1);
            if (quantity.IsFailure)
                return quantity.Error;
            if (lines[i].DiscountPercent == 0)
                continue;
            if (!mayDiscount)
                return SaleErrors.DiscountNotAllowed(i + 1);
            if (lines[i].DiscountPercent > maxDiscount)
                return SaleErrors.DiscountAboveMax(i + 1, maxDiscount);
        }

        var outcome = await pricing.PriceLinesAsync(
            lines.Select(l => new SalesPricingLine(l.ProductId, l.ProductUnitId, l.Quantity, l.DiscountPercent)).ToList(), ct);
        if (!outcome.Succeeded)
            return PricingErrors.Refused(outcome);
        var priced = new List<PosSaleLineInput>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var (product, unit) = (units.Value[i].Product, units.Value[i].Unit);
            var line = outcome.Lines[i];
            priced.Add(new PosSaleLineInput(
                product.Id, unit.Id, unit.UnitId, unit.ConversionFactor, product.Sku, product.NameAr, unit.UnitCode, lines[i].Quantity,
                line.UnitPrice, line.DiscountPercent, PosReferences.TreatmentOf(line.TaxTreatment), line.TaxRate, line.GrossAmount,
                line.DiscountAmount, line.NetAmount, line.TaxAmount, line.TotalAmount, line.NetUnitPrice));
        }
        return new PricedCart(outcome.CurrencyCode!, outcome.CurrencyDecimals, priced, maxDiscount);
    }

    // Whole where the unit can't be split, and an exact base quantity (whole where the base unit can't be split): Inventory
    // will be able to post it.
    private static Result CheckQuantity(decimal quantity, PosUnit unit, int line)
    {
        if (quantity <= 0 || quantity > Money.MaxQuantity || !Money.FitsScale(quantity, Money.QuantityScale))
            return SaleErrors.QuantityInvalid(line);
        if (!unit.Unit.UnitAllowsFraction && decimal.Truncate(quantity) != quantity)
            return SaleErrors.QuantityMustBeWhole(line);
        var baseQuantity = quantity * unit.Unit.ConversionFactor;
        if (!Money.FitsScale(baseQuantity, Money.QuantityScale)
            || (!unit.Unit.BaseUnitAllowsFraction && decimal.Truncate(baseQuantity) != baseQuantity))
            return SaleErrors.ConversionNotExact(line);
        return Result.Success();
    }
}
