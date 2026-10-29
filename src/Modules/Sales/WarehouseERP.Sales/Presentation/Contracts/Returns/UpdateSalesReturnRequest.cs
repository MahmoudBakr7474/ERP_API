using WarehouseERP.Sales.Application.Contracts.Returns;

namespace WarehouseERP.Sales.Presentation.Contracts.Returns;

/// <summary>A draft return's new reason, notes and lines.</summary>
public sealed record UpdateSalesReturnRequest(string Reason, string? Notes, IReadOnlyList<SalesReturnLineRequest> Lines, string RowVersion);
