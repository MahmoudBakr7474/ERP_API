namespace WarehouseERP.Purchasing.Presentation.Contracts.Documents;

/// <summary>An action with a reason (reject, close, cancel) from the version the client last read.</summary>
/// <param name="Reason">Required to reject or close; optional to cancel an order.</param>
public sealed record PurchasingReasonRequest(string? Reason, string RowVersion);
