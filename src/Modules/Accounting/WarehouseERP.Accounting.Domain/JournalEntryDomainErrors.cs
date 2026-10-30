using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

public static class JournalEntryDomainErrors
{
    public static readonly Error DescriptionInvalid = Error.BusinessRule(
        "Accounting.Journal.DescriptionInvalid", $"A journal needs a description of at most {JournalEntry.MaxDescriptionLength} characters.");

    public static readonly Error ReferenceTooLong = Error.BusinessRule(
        "Accounting.Journal.ReferenceTooLong", $"A journal's reference is at most {JournalEntry.MaxReferenceLength} characters.");

    public static readonly Error TooFewLines = Error.BusinessRule(
        "Accounting.Journal.TooFewLines", "A journal has at least two lines.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Accounting.Journal.TooManyLines", $"A journal has at most {JournalEntry.MaxLines} lines.");

    public static Error LineIncomplete(int line) => Error.BusinessRule(
        "Accounting.Journal.LineIncomplete", $"Line {line} needs an account and a branch.");

    public static Error LinePartyIncomplete(int line) => Error.BusinessRule(
        "Accounting.Journal.LinePartyIncomplete", $"Line {line}: a party needs both its type and its id, and only a line with a party has a due date.");

    public static readonly Error VoucherManaged = Error.BusinessRule(
        "Accounting.Journal.VoucherManaged", "This journal is a voucher: change it through its voucher, which rebuilds its lines.");

    public static Error LineOneSide(int line) => Error.BusinessRule(
        "Accounting.Journal.LineOneSide", $"Line {line} has an amount above zero on exactly one side, debit or credit.");

    public static Error LineAmountScale(int line, int decimals) => Error.BusinessRule(
        "Accounting.Journal.LineAmountScale", $"Line {line}'s amount has more than {decimals} decimals; amounts are never rounded silently.");

    public static Error LineAmountTooLarge(int line) => Error.BusinessRule(
        "Accounting.Journal.LineAmountTooLarge", $"Line {line}'s amount is above {Money.MaxLineAmount.ToString(CultureInfo.InvariantCulture)}.");

    public static Error LineDescriptionTooLong(int line) => Error.BusinessRule(
        "Accounting.Journal.LineDescriptionTooLong", $"Line {line}'s description is at most {JournalLine.MaxDescriptionLength} characters.");

    public static Error Unbalanced(decimal debit, decimal credit) => Error.BusinessRule(
        "Accounting.Journal.Unbalanced",
        string.Create(CultureInfo.InvariantCulture, $"Debits ({debit}) and credits ({credit}) differ by {debit - credit}; a journal must balance, and no balancing line is added for you."));

    public static readonly Error NotDraft = Error.BusinessRule(
        "Accounting.Journal.NotDraft", "Only a draft journal can do this.");

    public static readonly Error NotSubmitted = Error.BusinessRule(
        "Accounting.Journal.NotSubmitted", "The journal is not waiting for approval.");

    public static readonly Error NotOpen = Error.BusinessRule(
        "Accounting.Journal.NotOpen", "Only a draft or submitted journal can be cancelled; a posted journal is reversed instead.");

    public static readonly Error NotPosted = Error.BusinessRule(
        "Accounting.Journal.NotPosted", "Only a posted journal can be reversed.");

    public static readonly Error NotReversible = Error.BusinessRule(
        "Accounting.Journal.NotReversible", "Only manual, opening-balance and voucher journals are reversed by hand; a closing journal is reversed by reopening its year, and a reversal is never reversed.");

    public static readonly Error ApprovalRequired = Error.BusinessRule(
        "Accounting.Journal.ApprovalRequired", "This journal needs an approver: submit it, then approve it.");

    public static readonly Error ApprovalNotRequired = Error.BusinessRule(
        "Accounting.Journal.ApprovalNotRequired", "This journal needs no approval: post it directly.");

    public static readonly Error LinesFixed = Error.BusinessRule(
        "Accounting.Journal.LinesFixed", "A reversal's lines mirror the journal it reverses and can't be changed.");

    public static readonly Error ReasonRequired = Error.BusinessRule(
        "Accounting.Journal.ReasonRequired", $"Rejecting needs a reason of at most {JournalEntry.MaxNoteLength} characters.");
}
