using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.Journals;

/// <summary>What a voucher journal says beyond its lines.</summary>
/// <param name="Amount">Gross settlement for receipts and payments (cash plus tax withheld), money moved otherwise, total with VAT for a bill.</param>
public sealed record VoucherResponse(
    Guid? CashAccountId, string? CashAccountCode, Guid? CounterCashAccountId, string? CounterCashAccountCode, PartyType? PartyType,
    Guid? PartyId, string? PartyCode, string? PartyNameAr, PaymentMethod? PaymentMethod, decimal Amount, decimal? WithholdingAmount,
    decimal? TaxAmount, DateOnly? DueDate, IReadOnlyList<VoucherAllocationResponse> Allocations);
