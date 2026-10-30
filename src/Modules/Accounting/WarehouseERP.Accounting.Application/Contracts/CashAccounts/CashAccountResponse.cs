using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.CashAccounts;

/// <summary>A cashbox or bank account with its ledger account and posted balance (debit minus credit).</summary>
public sealed record CashAccountResponse(
    Guid Id, string Code, string Name, CashAccountKind Kind, Guid BranchId, string? BranchCode, string? BranchNameAr, Guid AccountId,
    string AccountCode, string AccountNameAr, string? BankName, string? BankAccountNumber, bool AllowsOverdraft, bool IsActive,
    decimal Balance, DateTime CreatedAtUtc, string RowVersion);
