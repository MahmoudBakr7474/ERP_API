namespace WarehouseERP.Pos.Application.Contracts.Terminals;

/// <summary>A terminal with its warehouse, selling location and the shift open on it, if any.</summary>
public sealed record PosTerminalResponse(
    Guid Id, string Code, string Name, Guid BranchId, string BranchCode, Guid WarehouseId, string WarehouseCode,
    Guid SellingLocationId, string SellingLocationCode, bool AcceptsCash, bool AcceptsCard, string? ReceiptHeader,
    string? ReceiptFooter, bool IsActive, Guid? OpenShiftId, string? OpenShiftNumber, Guid? OpenShiftCashierUserId,
    string? OpenShiftCashierName, string RowVersion, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
