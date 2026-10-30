using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Domain;

/// <summary>A cashbox or bank account a voucher moves money through, with its ledger account and branch.</summary>
public sealed record VoucherCash(Guid CashAccountId, CashAccountKind Kind, Guid AccountId, Guid BranchId);
