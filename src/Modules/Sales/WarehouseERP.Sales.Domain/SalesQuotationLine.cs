using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

/// <summary>One product of a quotation, priced as offered.</summary>
public sealed class SalesQuotationLine : PricedLine
{
    private SalesQuotationLine() { } // EF Core

    public Guid QuotationId { get; private set; }

    internal static Result<SalesQuotationLine> Create(int lineNumber, SalesLineInput input, int currencyDecimals)
    {
        var line = new SalesQuotationLine();
        var filled = line.Fill(lineNumber, input, currencyDecimals);
        return filled.IsFailure ? filled.Error : line;
    }
}
