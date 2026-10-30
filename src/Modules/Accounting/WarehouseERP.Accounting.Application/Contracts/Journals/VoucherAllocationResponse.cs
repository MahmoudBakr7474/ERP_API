namespace WarehouseERP.Accounting.Application.Contracts.Journals;

/// <summary>An open item a voucher settles when it posts.</summary>
public sealed record VoucherAllocationResponse(Guid OpenItemId, string? DocumentNumber, decimal Amount);
