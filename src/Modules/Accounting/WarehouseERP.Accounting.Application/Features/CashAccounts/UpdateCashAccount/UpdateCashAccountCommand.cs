namespace WarehouseERP.Accounting.Application.Features.CashAccounts.UpdateCashAccount;

/// <summary>What may change on a cashbox or bank account; its code, kind, branch and ledger account are fixed.</summary>
public sealed record UpdateCashAccountCommand(
    Guid CashAccountId, string Name, string? BankName, string? BankAccountNumber, bool AllowsOverdraft, string RowVersion);
