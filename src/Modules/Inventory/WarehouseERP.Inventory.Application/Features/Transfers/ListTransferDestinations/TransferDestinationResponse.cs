namespace WarehouseERP.Inventory.Application.Features.Transfers.ListTransferDestinations;

/// <summary>A warehouse a transfer can arrive in.</summary>
/// <param name="BranchCode">Its branch's code: warehouse codes repeat across branches.</param>
public sealed record TransferDestinationResponse(
    Guid WarehouseId, string Code, string NameAr, string? NameEn, Guid BranchId, string BranchCode);
