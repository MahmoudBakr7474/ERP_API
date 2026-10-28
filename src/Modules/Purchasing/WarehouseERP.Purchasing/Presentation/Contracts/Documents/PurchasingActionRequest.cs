namespace WarehouseERP.Purchasing.Presentation.Contracts.Documents;

/// <summary>An action on a purchasing document from the version the client last read.</summary>
public sealed record PurchasingActionRequest(string RowVersion);
