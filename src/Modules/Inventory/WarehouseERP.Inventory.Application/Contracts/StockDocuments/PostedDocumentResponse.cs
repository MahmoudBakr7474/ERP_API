using WarehouseERP.Inventory.Application.Contracts.Stock;

namespace WarehouseERP.Inventory.Application.Contracts.StockDocuments;

/// <summary>What posting (or approving) a document did; a retried request with the same key gets the same answer.</summary>
/// <param name="OverReservations">Warehouses this posting left holding less sellable stock than is reserved.</param>
public sealed record PostedDocumentResponse(
    Guid Id, string Number, DateTime PostedAtUtc, IReadOnlyList<OverReservationResponse> OverReservations);
