namespace WarehouseERP.Inventory.Application.Contracts.Reconciliation;

/// <summary>
/// Inventory checked against itself from one consistent snapshot. Lists differences; never corrects anything.
/// </summary>
/// <param name="IsConsistent">True when every difference list is empty.</param>
/// <param name="AvailabilityDifferences">Sellable balances of a physical warehouse vs. its OnHandSellable.</param>
/// <param name="LedgerDifferences">Posted movements vs. the balance of each location.</param>
/// <param name="ValuationDifferences">Company stock vs. the valuation's quantity, and the valuation vs. its entries.</param>
/// <param name="TransferDifferences">What open transfers still have in transit vs. their transit locations' balances.</param>
/// <param name="StockPositions">Per product: stock in physical warehouses, in transit, and valued.</param>
/// <param name="OpenTransfers">Transfers still waiting for stock to arrive or to be written off.</param>
/// <param name="ReservationDifferences">A warehouse's reserved quantity vs. what its sales order reservations still hold.</param>
public sealed record ReconciliationReport(
    DateTime GeneratedAtUtc, bool IsConsistent,
    IReadOnlyList<AvailabilityDifference> AvailabilityDifferences,
    IReadOnlyList<LedgerDifference> LedgerDifferences,
    IReadOnlyList<ValuationDifference> ValuationDifferences,
    IReadOnlyList<TransferDifference> TransferDifferences,
    IReadOnlyList<StockPosition> StockPositions,
    IReadOnlyList<OpenTransferPosition> OpenTransfers,
    IReadOnlyList<ReservationDifference> ReservationDifferences);
