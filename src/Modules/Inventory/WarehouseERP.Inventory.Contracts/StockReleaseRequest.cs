namespace WarehouseERP.Inventory.Contracts;

/// <summary>Reservations of a sales order to give back.</summary>
/// <param name="SourceDocumentId">The sales order that reserved them.</param>
/// <param name="Reason">Why (reduced, closed, cancelled), kept in the reservation's history.</param>
/// <param name="Lines">The order lines whose reservations give stock back.</param>
public sealed record StockReleaseRequest(Guid SourceDocumentId, string Reason, IReadOnlyList<StockReleaseLine> Lines);
