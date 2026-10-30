namespace WarehouseERP.Accounting.Domain;

/// <summary>A counter line of a cash voucher or an expense bill: an account, a branch and a positive amount; the voucher sets its side.</summary>
public sealed record VoucherLineInput(Guid AccountId, Guid BranchId, decimal Amount, string? Description);
