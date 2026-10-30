using System.Text.RegularExpressions;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// An account of the chart. Header accounts group their children and never take lines; posting accounts take lines and
/// report in their statement section. A child's code starts with its parent's (1 → 12 → 1201 → 120101) and it has its
/// parent's type. Code, parent, type, kind and party type are fixed once created; names, section and whether manual
/// journals may use it may change. Master data, versioned in a temporal table.
/// </summary>
public sealed partial class Account : MasterEntity
{
    public const int MaxCodeLength = 20;
    public const int MaxNameLength = 200;
    public const int MaxLevel = 8;

    private Account() { } // EF Core

    /// <summary>Digits only, unique, fixed.</summary>
    public string Code { get; private set; } = null!;

    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }

    /// <summary>The header account it belongs to; null for a top-level class.</summary>
    public Guid? ParentId { get; private set; }

    /// <summary>1 for a top-level class, its parent's level + 1 below it.</summary>
    public int Level { get; private set; }

    public AccountType Type { get; private set; }

    /// <summary>Where a posting account reports; optional on a header account.</summary>
    public StatementSection? Section { get; private set; }

    /// <summary>True for an account that takes journal lines; false for a header that groups others.</summary>
    public bool IsPosting { get; private set; }

    /// <summary>
    /// Whether manual journals may use it. False for accounts only the system posts to (inventory, GRNI, till clearing):
    /// their balance must come from their source documents.
    /// </summary>
    public bool AllowsManualPosting { get; private set; }

    /// <summary>Set on a control account: each of its lines names a customer or supplier.</summary>
    public PartyType? PartyType { get; private set; }

    public static Result<Account> Create(
        string code, string nameAr, string? nameEn, Account? parent, AccountType type, StatementSection? section, bool isPosting,
        bool allowsManualPosting, PartyType? partyType)
    {
        var normalized = (code ?? "").Trim();
        if (!CodePattern().IsMatch(normalized))
            return AccountDomainErrors.CodeInvalid;
        if (!Enum.IsDefined(type))
            return AccountDomainErrors.TypeInvalid;
        if (parent is not null)
        {
            if (parent.Id == Guid.Empty)
                throw new InvalidOperationException("A parent account must be added to the context before its children.");
            if (parent.IsPosting)
                return AccountDomainErrors.ParentIsPosting;
            if (!parent.IsActive)
                return AccountDomainErrors.ParentNotActive;
            if (normalized.Length <= parent.Code.Length || !normalized.StartsWith(parent.Code, StringComparison.Ordinal))
                return AccountDomainErrors.CodeOutsideParent;
            if (parent.Type != type)
                return AccountDomainErrors.TypeDiffersFromParent;
            if (parent.Level >= MaxLevel)
                return AccountDomainErrors.TooDeep;
        }
        if (partyType is not null)
        {
            if (!isPosting || !Enum.IsDefined(partyType.Value))
                return AccountDomainErrors.PartyInvalid;
            if ((partyType == Contracts.PartyType.Customer && type != AccountType.Asset)
                || (partyType == Contracts.PartyType.Supplier && type != AccountType.Liability))
                return AccountDomainErrors.PartyInvalid;
        }

        var account = new Account
        {
            Code = normalized,
            ParentId = parent?.Id,
            Level = parent is null ? 1 : parent.Level + 1,
            Type = type,
            IsPosting = isPosting,
            PartyType = partyType,
        };
        var updated = account.Update(nameAr, nameEn, section, allowsManualPosting);
        return updated.IsFailure ? updated.Error : account;
    }

    public Result Update(string nameAr, string? nameEn, StatementSection? section, bool allowsManualPosting)
    {
        var arabic = (nameAr ?? "").Trim();
        if (arabic.Length is 0 or > MaxNameLength)
            return AccountDomainErrors.NameInvalid;
        var english = Text.Optional(nameEn);
        if (english?.Length > MaxNameLength)
            return AccountDomainErrors.NameInvalid;
        if (section is null)
        {
            if (IsPosting)
                return AccountDomainErrors.SectionRequired;
        }
        else if (!Fits(Type, section.Value))
            return AccountDomainErrors.SectionDoesNotFitType;

        NameAr = arabic;
        NameEn = english;
        Section = section;
        AllowsManualPosting = IsPosting && allowsManualPosting;
        return Result.Success();
    }

    /// <summary>Whether a statement section can hold accounts of this type.</summary>
    public static bool Fits(AccountType type, StatementSection section) => type switch
    {
        AccountType.Asset => section is StatementSection.CurrentAssets or StatementSection.NonCurrentAssets,
        AccountType.Liability => section is StatementSection.CurrentLiabilities or StatementSection.NonCurrentLiabilities,
        AccountType.Equity => section is StatementSection.Equity,
        AccountType.Revenue => section is StatementSection.Revenue or StatementSection.OtherIncome,
        AccountType.Expense => section is StatementSection.CostOfSales or StatementSection.OperatingExpenses
            or StatementSection.OtherExpenses or StatementSection.IncomeTax,
        _ => false,
    };

    /// <summary>True for revenue and expense accounts, whose balances a year-end close moves into retained earnings.</summary>
    public bool IsIncomeStatement => Type is AccountType.Revenue or AccountType.Expense;

    [GeneratedRegex("^[0-9]{1,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
