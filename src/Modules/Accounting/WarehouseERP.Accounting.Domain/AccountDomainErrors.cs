using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

public static class AccountDomainErrors
{
    public static readonly Error CodeInvalid = Error.BusinessRule(
        "Accounting.Account.CodeInvalid", $"An account code is 1 to {Account.MaxCodeLength} digits.");

    public static readonly Error NameInvalid = Error.BusinessRule(
        "Accounting.Account.NameInvalid", $"An account's Arabic name is 1 to {Account.MaxNameLength} characters; its English name at most {Account.MaxNameLength}.");

    public static readonly Error TypeInvalid = Error.BusinessRule(
        "Accounting.Account.TypeInvalid", "The account type is unknown.");

    public static readonly Error ParentIsPosting = Error.BusinessRule(
        "Accounting.Account.ParentIsPosting", "A posting account can't have children; choose a header account as the parent.");

    public static readonly Error ParentNotActive = Error.BusinessRule(
        "Accounting.Account.ParentNotActive", "The parent account is inactive.");

    public static readonly Error CodeOutsideParent = Error.BusinessRule(
        "Accounting.Account.CodeOutsideParent", "A child account's code starts with its parent's code and is longer.");

    public static readonly Error TypeDiffersFromParent = Error.BusinessRule(
        "Accounting.Account.TypeDiffersFromParent", "A child account has the same type as its parent.");

    public static readonly Error TooDeep = Error.BusinessRule(
        "Accounting.Account.TooDeep", $"The chart is at most {Account.MaxLevel} levels deep.");

    public static readonly Error SectionRequired = Error.BusinessRule(
        "Accounting.Account.SectionRequired", "A posting account needs the statement section it reports in.");

    public static readonly Error SectionDoesNotFitType = Error.BusinessRule(
        "Accounting.Account.SectionDoesNotFitType", "The statement section does not fit the account type (assets, liabilities, equity, revenue or expenses).");

    public static readonly Error PartyInvalid = Error.BusinessRule(
        "Accounting.Account.PartyInvalid", "Only a posting account is a control account: customers on an asset account, suppliers on a liability account.");
}
