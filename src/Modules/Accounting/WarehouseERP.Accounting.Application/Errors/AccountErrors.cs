using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class AccountErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Accounting.Account.NotFound", "The account does not exist.");

    public static readonly Error CodeTaken = Error.Conflict(
        "Accounting.Account.CodeTaken", "Another account already has this code.");

    public static readonly Error ParentNotFound = Error.BusinessRule(
        "Accounting.Account.ParentNotFound", "The parent account does not exist.");

    public static readonly Error HasChildren = Error.BusinessRule(
        "Accounting.Account.HasChildren", "The account has child accounts; delete them first.");

    public static readonly Error HasActiveChildren = Error.BusinessRule(
        "Accounting.Account.HasActiveChildren", "Deactivate the account's active child accounts first.");

    public static readonly Error HasLines = Error.BusinessRule(
        "Accounting.Account.HasLines", "Journals use the account; deactivate it instead of deleting it.");

    public static readonly Error HasBalance = Error.BusinessRule(
        "Accounting.Account.HasBalance", "The account has a posted balance; clear it with a journal before deactivating it.");

    public static readonly Error Mapped = Error.BusinessRule(
        "Accounting.Account.Mapped", "The account is mapped to a posting role; map the role to another account first.");

    public static readonly Error ChartNotEmpty = Error.Conflict(
        "Accounting.Chart.NotEmpty", "The chart of accounts already has accounts; a chart is imported only into an empty chart.");

    public static Error ImportCodeDuplicate(string code) => Error.BusinessRule(
        "Accounting.Chart.CodeDuplicate", $"Account code {code} appears more than once.");

    public static Error ImportParentMissing(string code, string parentCode) => Error.BusinessRule(
        "Accounting.Chart.ParentMissing", $"Account {code} names parent {parentCode}, which is not in the chart.");

    public static Error ImportRoleAccountMissing(string role, string code) => Error.BusinessRule(
        "Accounting.Chart.RoleAccountMissing", $"Role {role} names account {code}, which is not in the chart.");

    /// <summary>A domain refusal of one imported row: its code is kept and the row is named in the message.</summary>
    public static Error ImportRowRefused(string code, Error refusal) => Error.BusinessRule(refusal.Code, $"Account {code}: {refusal.Message}");
}
