namespace WarehouseERP.Sales.Presentation.Contracts.Documents;

/// <summary>An action with a reason (reject, decline, close, cancel) from the version the client last read.</summary>
/// <param name="Reason">Required to reject, decline or close; optional to cancel.</param>
public sealed record SalesReasonRequest(string? Reason, string RowVersion);
