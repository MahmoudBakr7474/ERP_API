using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

/// <summary>One billed quantity of an order line, at the order line's agreed price, discount and tax (copied, never re-priced).</summary>
public sealed class SalesInvoiceLine : PricedLine
{
    private SalesInvoiceLine() { } // EF Core

    public Guid InvoiceId { get; private set; }
    public Guid OrderLineId { get; private set; }

    internal static Result<SalesInvoiceLine> Create(int lineNumber, InvoiceLineInput input, SalesOrderLine orderLine, int currencyDecimals)
    {
        var line = new SalesInvoiceLine { OrderLineId = orderLine.Id };
        var filled = line.Fill(lineNumber,
            new SalesLineInput(input.Unit, input.Quantity, orderLine.Price, orderLine.PriceSource, orderLine.ListUnitPrice), currencyDecimals);
        return filled.IsFailure ? filled.Error : line;
    }
}
