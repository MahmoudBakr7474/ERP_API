using WarehouseERP.Sales.Application.Contracts.Returns;

namespace WarehouseERP.Sales.Application.Features.Returns.UpdateSalesReturn;

/// <summary>Replaces a draft return's reason, notes and lines.</summary>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record UpdateSalesReturnCommand(Guid ReturnId, string Reason, string? Notes, IReadOnlyList<SalesReturnLineRequest> Lines, string RowVersion);
