namespace WarehouseERP.Accounting.Domain;

/// <summary>An open item of the voucher's party to settle by this much when the voucher posts.</summary>
public sealed record VoucherAllocationInput(Guid OpenItemId, decimal Amount);
