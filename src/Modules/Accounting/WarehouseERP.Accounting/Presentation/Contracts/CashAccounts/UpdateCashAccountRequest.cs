namespace WarehouseERP.Accounting.Presentation.Contracts.CashAccounts;

/// <summary>What may change on a cashbox or bank account; its code, kind, branch and ledger account are fixed.</summary>
/// <param name="ReceivesPosCash">A cashbox only: the branch's one cashbox that closed POS shifts hand their cash in to.</param>
public sealed record UpdateCashAccountRequest(
    string Name, string? BankName, string? BankAccountNumber, bool AllowsOverdraft, string RowVersion, bool ReceivesPosCash = false);
