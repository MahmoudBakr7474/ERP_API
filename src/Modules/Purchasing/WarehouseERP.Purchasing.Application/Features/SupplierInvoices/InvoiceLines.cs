using FluentValidation;
using WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices;

// An invoice's lines as the client sends them, shared by the create and update use cases. Matching to the order and
// the amounts are the domain's.
internal static class InvoiceLines
{
    public static void Rules(InlineValidator<InvoiceLineRequest> line)
    {
        line.RuleLevelCascadeMode = CascadeMode.Stop;
        line.RuleFor(x => x.PurchaseOrderLineId).NotEmpty();
        line.RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(Amounts.MaxQuantity)
            .Must(q => Amounts.FitsScale(q, Amounts.QuantityScale)).WithMessage("'Quantity' may have at most 4 decimals.");
        line.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).LessThanOrEqualTo(Amounts.MaxUnitPrice)
            .Must(p => Amounts.FitsScale(p, Amounts.PriceScale)).WithMessage("'Unit Price' may have at most 8 decimals.");
        line.RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100)
            .Must(p => Amounts.FitsScale(p, Amounts.PercentScale)).WithMessage("'Discount Percent' may have at most 4 decimals.");
        line.RuleFor(x => x.TaxRate).InclusiveBetween(0, 100)
            .Must(p => Amounts.FitsScale(p, Amounts.PercentScale)).WithMessage("'Tax Rate' may have at most 4 decimals.");
        line.RuleFor(x => x.TaxTreatment).IsInEnum();
    }

    public static IReadOnlyList<InvoiceLineInput> Inputs(IReadOnlyList<InvoiceLineRequest> lines) =>
        lines.Select(l => new InvoiceLineInput(
            l.PurchaseOrderLineId, l.Quantity, new LinePrice(l.UnitPrice, l.DiscountPercent, l.TaxRate, l.TaxTreatment))).ToList();
}
