using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class BankReconciliationErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Accounting.BankReconciliation.NotFound", "The bank reconciliation does not exist.");

    public static readonly Error NotBank = Error.BusinessRule(
        "Accounting.BankReconciliation.NotBank", "Only a bank account is reconciled with a bank statement.");

    public static readonly Error DraftExists = Error.Conflict(
        "Accounting.BankReconciliation.DraftExists", "This bank account already has a reconciliation in progress.");

    public static readonly Error BeforeLastStatement = Error.BusinessRule(
        "Accounting.BankReconciliation.BeforeLastStatement", "The statement date is before the last completed reconciliation's.");

    public static readonly Error LineNotFree = Error.BusinessRule(
        "Accounting.BankReconciliation.LineNotFree",
        "A ticked line is not a posted line of this bank account dated on or before the statement, or another reconciliation holds it.");

    public static readonly Error LineTicked = Error.Conflict(
        "Accounting.BankReconciliation.LineTicked", "Another reconciliation ticked one of these lines at the same moment.");
}
