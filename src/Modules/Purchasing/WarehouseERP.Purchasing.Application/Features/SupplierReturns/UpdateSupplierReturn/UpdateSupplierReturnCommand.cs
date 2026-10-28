using WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.UpdateSupplierReturn;

/// <summary>Replaces a draft's reason, notes and lines; its goods receipt stays.</summary>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record UpdateSupplierReturnCommand(
    Guid SupplierReturnId, string Reason, string? Notes, IReadOnlyList<ReturnLineRequest> Lines, string RowVersion);
