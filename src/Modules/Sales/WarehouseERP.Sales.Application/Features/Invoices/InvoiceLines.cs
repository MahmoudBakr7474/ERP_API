using FluentValidation;
using WarehouseERP.Sales.Application.Contracts.Invoices;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Invoices;

// An invoice's lines as the client sends them: their shape (shared by the create and update validators), the order line's
// product unit with Catalog's fraction rules (active or not: delivered goods are still billed), and the invoice date, which
// can't be after today in the company's time zone.
internal static class InvoiceLines
{
    public static void Rules(InlineValidator<SalesInvoiceLineRequest> line)
    {
        line.RuleLevelCascadeMode = CascadeMode.Stop;
        line.RuleFor(x => x.OrderLineId).NotEmpty();
        line.RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(Amounts.MaxQuantity)
            .Must(q => Amounts.FitsScale(q, Amounts.QuantityScale)).WithMessage("'Quantity' may have at most 4 decimals.");
    }

    public static async Task<Result<IReadOnlyList<InvoiceLineInput>>> ResolveAsync(
        SalesReferences references, SalesOrder order, IReadOnlyList<SalesInvoiceLineRequest> lines, CancellationToken ct)
    {
        var orderLines = new List<SalesOrderLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            if (order.FindLine(lines[i].OrderLineId) is not { } orderLine)
                return SalesOrderDomainErrors.LineNotFound(i + 1);
            orderLines.Add(orderLine);
        }
        var units = await references.UnitFactsAsync(orderLines.Select(l => l.ProductUnitId).ToList(), ct);
        return lines.Select((l, i) => new InvoiceLineInput(l.OrderLineId, units[orderLines[i].ProductUnitId], l.Quantity)).ToList();
    }

    /// <summary>The invoice date: the one given, never after today; today when left out.</summary>
    public static async Task<Result<DateOnly>> DateAsync(SalesReferences references, DateOnly? invoiceDate, CancellationToken ct)
    {
        var today = await references.TodayAsync(ct);
        if (today.IsFailure)
            return today.Error;
        var date = invoiceDate ?? today.Value;
        return date > today.Value ? InvoiceErrors.DateInFuture : date;
    }
}
