using WarehouseERP.Accounting.Application.Contracts.Vouchers;
using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Vouchers.UpdateVoucher;

/// <summary>Replaces a voucher draft as a whole; its type is fixed.</summary>
public sealed record UpdateVoucherCommand(
    Guid JournalId, DateOnly PostingDate, string Description, string? Reference, Guid? CashAccountId, Guid? CounterCashAccountId,
    PartyType? PartyType, Guid? PartyId, PaymentMethod? PaymentMethod, decimal Amount, decimal WithholdingAmount, decimal TaxAmount,
    DateOnly? DueDate, Guid? BranchId, IReadOnlyList<VoucherLineRequest>? Lines, IReadOnlyList<VoucherAllocationRequest>? Allocations,
    string RowVersion);
