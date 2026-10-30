using WarehouseERP.Accounting.Application.Contracts.Vouchers;
using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Vouchers;

/// <summary>What a voucher draft says, whether it is being created or changed.</summary>
public sealed record VoucherFields(
    JournalType Type, DateOnly PostingDate, string Description, string? Reference, Guid? CashAccountId, Guid? CounterCashAccountId,
    PartyType? PartyType, Guid? PartyId, PaymentMethod? PaymentMethod, decimal Amount, decimal WithholdingAmount, decimal TaxAmount,
    DateOnly? DueDate, Guid? BranchId, IReadOnlyList<VoucherLineRequest> Lines, IReadOnlyList<VoucherAllocationRequest> Allocations);
