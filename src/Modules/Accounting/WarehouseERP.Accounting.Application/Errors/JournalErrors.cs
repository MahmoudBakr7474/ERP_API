using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class JournalErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Accounting.Journal.NotFound", "The journal does not exist.");

    public static Error AccountNotFound(int line) => Error.BusinessRule(
        "Accounting.Journal.AccountNotFound", $"Line {line}: the account does not exist.");

    public static Error AccountNotPosting(int line) => Error.BusinessRule(
        "Accounting.Journal.AccountNotPosting", $"Line {line}: the account is a header account; journals post to posting accounts only.");

    public static Error AccountNotActive(int line) => Error.BusinessRule(
        "Accounting.Journal.AccountNotActive", $"Line {line}: the account is inactive.");

    public static Error AccountNotManual(int line) => Error.BusinessRule(
        "Accounting.Journal.AccountNotManual", $"Line {line}: the account takes only system postings and opening balances, not manual journals.");

    public static Error BranchNotFound(int line) => Error.BusinessRule(
        "Accounting.Journal.BranchNotFound", $"Line {line}: the branch does not exist.");

    public static Error BranchNotActive(int line) => Error.BusinessRule(
        "Accounting.Journal.BranchNotActive", $"Line {line}: the branch is inactive.");

    public static Error PeriodMissing(DateOnly date) => Error.BusinessRule(
        "Accounting.Journal.PeriodMissing",
        string.Create(CultureInfo.InvariantCulture, $"No fiscal period covers {date:yyyy-MM-dd}; create its fiscal year first."));

    public static Error PeriodClosed(DateOnly date) => Error.BusinessRule(
        "Accounting.Journal.PeriodClosed",
        string.Create(CultureInfo.InvariantCulture, $"The period of {date:yyyy-MM-dd} is closed: nothing posts into it, and nothing is moved to another period for you."));

    public static Error BeforeCutover(DateOnly cutover) => Error.BusinessRule(
        "Accounting.Journal.BeforeCutover",
        string.Create(CultureInfo.InvariantCulture, $"Journals are dated on or after the accounting start date {cutover:yyyy-MM-dd}."));

    public static Error OpeningDateMismatch(DateOnly cutover) => Error.BusinessRule(
        "Accounting.Journal.OpeningDateMismatch",
        string.Create(CultureInfo.InvariantCulture, $"Opening balances are dated on the accounting start date {cutover:yyyy-MM-dd}; save the draft again to take it."));

    public static readonly Error AlreadyReversed = Error.Conflict(
        "Accounting.Journal.AlreadyReversed", "The journal already has a reversal (drafted, submitted or posted).");

    public static readonly Error ReversalBeforeOriginal = Error.BusinessRule(
        "Accounting.Journal.ReversalBeforeOriginal", "A reversal is dated on or after the journal it reverses.");

    public static Error PartyRequired(int line) => Error.BusinessRule(
        "Accounting.Journal.PartyRequired", $"Line {line}: the account is a customer or supplier control account; name the party of this line.");

    public static Error PartyNotAllowed(int line) => Error.BusinessRule(
        "Accounting.Journal.PartyNotAllowed", $"Line {line}: only a control account line names a party, and of the account's own kind.");

    public static Error PartyNotFound(int line) => Error.BusinessRule(
        "Accounting.Journal.PartyNotFound", $"Line {line}: the customer or supplier does not exist.");

    public static readonly Error ItemsAllocated = Error.BusinessRule(
        "Accounting.Journal.ItemsAllocated", "The journal's open items are allocated; remove those allocations before reversing it.");

    public static Error CashAccountOverdrawn(string code) => Error.BusinessRule(
        "Accounting.Journal.CashAccountOverdrawn", $"Posting would take {code} below zero; a cashbox never goes negative, a bank account only with an overdraft.");

    public static readonly Error Busy = Error.Conflict(
        "Accounting.Journal.Busy", "A customer, supplier, cashbox or bank account of this journal is being posted to; try again in a moment.");

    public static readonly Error PeriodBusy = Error.Conflict(
        "Accounting.Journal.PeriodBusy", "The period is being closed or reopened; try again in a moment.");
}
