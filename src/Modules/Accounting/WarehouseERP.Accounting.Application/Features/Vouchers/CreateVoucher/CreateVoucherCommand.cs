using WarehouseERP.Accounting.Application.Contracts.Vouchers;
using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Vouchers.CreateVoucher;

/// <summary>
/// A voucher draft: a customer receipt or refund, a supplier payment or refund, a cash receipt or payment, a transfer or an
/// expense bill. Its journal lines are built from it; it posts, is approved, cancelled and reversed like any journal.
/// </summary>
/// <param name="Reference">The cheque or transfer number, or the supplier's bill number.</param>
/// <param name="Amount">Net cash received or paid (tax withheld comes on top), or the refund or transfer amount; cash vouchers and bills take their total from their lines.</param>
/// <param name="WithholdingAmount">Tax withheld on a receipt (by the customer) or a payment (by the company), entered by hand.</param>
/// <param name="TaxAmount">Recoverable VAT on an expense bill.</param>
/// <param name="DueDate">An expense bill's due date; its supplier's payment terms by default.</param>
/// <param name="BranchId">The branch of the party, tax and withholding lines; the cash account's branch by default.</param>
/// <param name="Allocations">Open items of the party a receipt, payment or refund settles when it posts.</param>
public sealed record CreateVoucherCommand(
    JournalType Type, DateOnly PostingDate, string Description, string? Reference, Guid? CashAccountId, Guid? CounterCashAccountId,
    PartyType? PartyType, Guid? PartyId, PaymentMethod? PaymentMethod, decimal Amount, decimal WithholdingAmount, decimal TaxAmount,
    DateOnly? DueDate, Guid? BranchId, IReadOnlyList<VoucherLineRequest>? Lines, IReadOnlyList<VoucherAllocationRequest>? Allocations);
