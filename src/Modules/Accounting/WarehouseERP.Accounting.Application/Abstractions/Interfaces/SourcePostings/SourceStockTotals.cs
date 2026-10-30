namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;

/// <summary>Sums over journaled documents: what Inventory's cost entries moved (exact) and what journals posted for it.</summary>
public sealed record SourceStockTotals(decimal StockValue, decimal PostedStockValue);
