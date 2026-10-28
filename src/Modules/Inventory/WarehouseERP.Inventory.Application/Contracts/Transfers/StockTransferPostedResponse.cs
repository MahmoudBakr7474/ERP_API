using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.Transfers;

/// <summary>What dispatching (or approving) or receiving a transfer posted; a retry with the same key gets the same answer.</summary>
/// <param name="Number">The posted document's number (TRF-… for a dispatch, TRR-… for a receipt).</param>
/// <param name="Status">The transfer's status afterwards.</param>
public sealed record StockTransferPostedResponse(
    Guid TransferId, Guid DocumentId, string Number, StockTransferStatus Status, DateTime PostedAtUtc,
    IReadOnlyList<OverReservationResponse> OverReservations);
