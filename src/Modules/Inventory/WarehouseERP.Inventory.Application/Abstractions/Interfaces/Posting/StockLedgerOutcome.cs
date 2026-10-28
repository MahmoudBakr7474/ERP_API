namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

/// <param name="Shortage">The first change there was not enough stock for; null when everything applied.</param>
/// <param name="OverReserved">Warehouses left over-reserved by an authorized decrease (already recorded as alerts).</param>
public sealed record StockLedgerOutcome(StockShortage? Shortage, IReadOnlyList<OverReservedStock> OverReserved);
