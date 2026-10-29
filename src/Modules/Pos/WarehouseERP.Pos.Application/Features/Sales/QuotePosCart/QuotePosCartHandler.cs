using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Sales.QuotePosCart;

// The checkout's pricing and checks without recording anything, plus a stock hint per line: what the till shows before it
// asks the card terminal for money, so a cart that can't be sold is found before a card is charged.
public sealed class QuotePosCartHandler(TillAccess tills, CartPricing cart, IStockAvailabilityLookup stock)
    : IQueryHandler<QuotePosCartQuery, PosCartQuoteResponse>
{
    public async Task<Result<PosCartQuoteResponse>> HandleAsync(QuotePosCartQuery query, CancellationToken ct)
    {
        var terminal = await tills.UsableAsync(query.TerminalId, ct);
        if (terminal.IsFailure)
            return terminal.Error;
        var priced = await cart.PriceAsync(query.Lines, ct);
        if (priced.IsFailure)
            return priced.Error;
        var lines = priced.Value.Lines;
        var available = (await stock.GetAsync(terminal.Value.WarehouseId, terminal.Value.SellingLocationId,
                lines.Select(l => l.ProductId).Distinct().ToList(), ct))
            .ToDictionary(a => a.ProductId, a => a.Sellable);
        var needed = lines.GroupBy(l => l.ProductId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity * l.ConversionFactor));
        var decimals = priced.Value.CurrencyDecimals;

        var quoted = lines.Select((l, i) =>
            {
                var sellable = available.GetValueOrDefault(l.ProductId);
                return new PosCartQuoteLineResponse(
                    i + 1, l.ProductId, l.Sku, l.ProductName, l.ProductUnitId, l.UnitCode, l.Quantity, l.UnitPrice,
                    PosReferences.WithTax(l.UnitPrice, l.TaxRate, decimals), l.DiscountPercent, l.GrossAmount, l.DiscountAmount,
                    l.NetAmount, l.TaxTreatment, l.TaxRate, l.TaxAmount, l.TotalAmount, sellable, needed[l.ProductId] > sellable);
            })
            .ToList();
        return new PosCartQuoteResponse(
            priced.Value.CurrencyCode, decimals, priced.Value.MaxLineDiscountPercent, quoted, lines.Sum(l => l.GrossAmount),
            lines.Sum(l => l.DiscountAmount), lines.Sum(l => l.NetAmount), lines.Sum(l => l.TaxAmount), lines.Sum(l => l.TotalAmount),
            quoted.Any(l => l.StockShort));
    }
}
