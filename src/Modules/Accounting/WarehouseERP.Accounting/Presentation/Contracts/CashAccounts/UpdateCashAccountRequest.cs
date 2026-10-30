namespace WarehouseERP.Accounting.Presentation.Contracts.CashAccounts;

/// <summary>What may change on a cashbox or bank account; its code, kind, branch and ledger account are fixed.</summary>
public sealed record UpdateCashAccountRequest(string Name, string? BankName, string? BankAccountNumber, bool AllowsOverdraft, string RowVersion);
