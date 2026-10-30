using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// What a voucher says, with the accounts the application resolved for it: its cash account (and a transfer's
/// destination), its party and that party's control account, its payment method, amounts and tax, its counter lines
/// (cash vouchers and expense bills) and the open items it settles.
/// </summary>
/// <param name="BranchId">The branch of the party, tax and withholding lines.</param>
/// <param name="Amount">
/// The money moved: net cash received or paid for receipts and payments (tax withheld comes on top), the refund or
/// transfer amount. Cash vouchers and expense bills take their total from their lines.
/// </param>
/// <param name="DueDate">When an expense bill falls due.</param>
public sealed record VoucherInput(
    JournalType Type, DateOnly PostingDate, string Description, string? Reference, string CurrencyCode, bool RequiresApproval,
    Guid BranchId, VoucherCash? Cash, VoucherCash? CounterCash, VoucherParty? Party, PaymentMethod? PaymentMethod, decimal Amount,
    decimal WithholdingAmount, Guid? WithholdingAccountId, decimal TaxAmount, Guid? TaxAccountId, DateOnly? DueDate,
    IReadOnlyList<VoucherLineInput> Lines, IReadOnlyList<VoucherAllocationInput> Allocations);
