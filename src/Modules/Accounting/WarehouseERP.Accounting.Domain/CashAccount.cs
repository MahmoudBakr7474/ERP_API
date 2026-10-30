using System.Text.RegularExpressions;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// A cashbox or bank account of a branch, kept on its own posting account of the ledger (one treasury account per ledger
/// account), so its balance is always that account's posted balance. A cashbox never goes below zero; a bank account only
/// when it allows an overdraft. Code, kind, branch and ledger account are fixed. Master data, versioned in a temporal table.
/// </summary>
public sealed partial class CashAccount : MasterEntity
{
    public const int MaxCodeLength = 20;
    public const int MaxNameLength = 100;
    public const int MaxBankNameLength = 100;
    public const int MaxAccountNumberLength = 50;

    private CashAccount() { } // EF Core

    /// <summary>Unique and fixed: upper-case letters, digits and dashes.</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;
    public CashAccountKind Kind { get; private set; }
    public Guid BranchId { get; private set; }

    /// <summary>The ledger posting account that holds its money.</summary>
    public Guid AccountId { get; private set; }

    public string? BankName { get; private set; }

    /// <summary>The bank account number or IBAN.</summary>
    public string? BankAccountNumber { get; private set; }

    public bool AllowsOverdraft { get; private set; }

    public static Result<CashAccount> Create(
        string code, string name, CashAccountKind kind, Guid branchId, Account account, string? bankName, string? bankAccountNumber,
        bool allowsOverdraft)
    {
        var normalized = (code ?? "").Trim().ToUpperInvariant();
        if (!CodePattern().IsMatch(normalized))
            return CashAccountDomainErrors.CodeInvalid;
        if (!Enum.IsDefined(kind))
            return CashAccountDomainErrors.KindInvalid;
        if (!account.IsPosting || !account.IsActive || account.Type != AccountType.Asset || account.PartyType is not null)
            return CashAccountDomainErrors.AccountInvalid;
        var cash = new CashAccount { Code = normalized, Kind = kind, BranchId = branchId, AccountId = account.Id };
        var updated = cash.Update(name, bankName, bankAccountNumber, allowsOverdraft);
        return updated.IsFailure ? updated.Error : cash;
    }

    public Result Update(string name, string? bankName, string? bankAccountNumber, bool allowsOverdraft)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length is 0 or > MaxNameLength)
            return CashAccountDomainErrors.NameInvalid;
        var bank = Text.Optional(bankName);
        var number = Text.Optional(bankAccountNumber);
        if (Kind == CashAccountKind.Cashbox && (bank is not null || number is not null || allowsOverdraft))
            return CashAccountDomainErrors.BankDetailsOnCashbox;
        if (bank?.Length > MaxBankNameLength || number?.Length > MaxAccountNumberLength)
            return CashAccountDomainErrors.BankDetailsTooLong;
        Name = trimmed;
        BankName = bank;
        BankAccountNumber = number;
        AllowsOverdraft = allowsOverdraft;
        return Result.Success();
    }

    /// <summary>Whether a balance below zero is refused.</summary>
    public bool MustStayPositive => Kind == CashAccountKind.Cashbox || !AllowsOverdraft;

    [GeneratedRegex("^[A-Z0-9][A-Z0-9-]{0,19}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
