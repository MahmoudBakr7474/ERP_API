namespace WarehouseERP.Sales.Presentation.Contracts.Documents;

/// <summary>An action on a sales document from the version the client last read.</summary>
public sealed record SalesActionRequest(string RowVersion);
