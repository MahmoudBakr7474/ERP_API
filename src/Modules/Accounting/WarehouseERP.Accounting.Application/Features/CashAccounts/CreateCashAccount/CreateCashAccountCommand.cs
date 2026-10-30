using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.CashAccounts.CreateCashAccount;

/// <param name="AccountId">The ledger posting account (an asset) that holds its money; one cash account per ledger account.</param>
/// <param name="AllowsOverdraft">A bank account only.</param>
/// <param name="ReceivesPosCash">A cashbox only: the branch's one cashbox that closed POS shifts hand their cash in to.</param>
public sealed record CreateCashAccountCommand(
    string Code, string Name, CashAccountKind Kind, Guid BranchId, Guid AccountId, string? BankName, string? BankAccountNumber,
    bool AllowsOverdraft, bool ReceivesPosCash = false);
