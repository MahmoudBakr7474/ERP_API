using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

public static class BankReconciliationDomainErrors
{
    public static readonly Error NotDraft = Error.BusinessRule(
        "Accounting.BankReconciliation.NotDraft", "Only a draft reconciliation can change, complete or be cancelled.");

    public static Error NotBalanced(decimal difference) => Error.BusinessRule(
        "Accounting.BankReconciliation.NotBalanced",
        string.Create(CultureInfo.InvariantCulture, $"The cleared lines differ from the statement by {difference}; tick the missing lines or post the bank's charges first."));
}
