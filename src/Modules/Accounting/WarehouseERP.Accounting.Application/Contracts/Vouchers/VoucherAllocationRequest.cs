namespace WarehouseERP.Accounting.Application.Contracts.Vouchers;

/// <summary>An open item of the voucher's party to settle by this much when the voucher posts.</summary>
public sealed record VoucherAllocationRequest(Guid OpenItemId, decimal Amount);
