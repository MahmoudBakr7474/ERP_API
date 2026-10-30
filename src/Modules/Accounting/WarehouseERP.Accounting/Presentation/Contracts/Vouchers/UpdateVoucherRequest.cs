using WarehouseERP.Accounting.Application.Contracts.Vouchers;
using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Presentation.Contracts.Vouchers;

/// <summary>A voucher draft replaced as a whole; its type is fixed.</summary>
public sealed record UpdateVoucherRequest(
    DateOnly PostingDate, string Description, string? Reference, Guid? CashAccountId, Guid? CounterCashAccountId, PartyType? PartyType,
    Guid? PartyId, PaymentMethod? PaymentMethod, decimal Amount, decimal WithholdingAmount, decimal TaxAmount, DateOnly? DueDate,
    Guid? BranchId, IReadOnlyList<VoucherLineRequest>? Lines, IReadOnlyList<VoucherAllocationRequest>? Allocations, string RowVersion);
