namespace WarehouseERP.Accounting.Application.Contracts.Vouchers;

/// <summary>A counter line of a cash voucher or an expense bill: an account, a branch and a positive amount.</summary>
public sealed record VoucherLineRequest(Guid AccountId, Guid BranchId, decimal Amount, string? Description);
