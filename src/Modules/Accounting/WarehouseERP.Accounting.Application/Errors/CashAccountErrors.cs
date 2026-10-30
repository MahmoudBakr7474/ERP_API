using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class CashAccountErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Accounting.CashAccount.NotFound", "The cashbox or bank account does not exist.");

    public static readonly Error CodeTaken = Error.Conflict(
        "Accounting.CashAccount.CodeTaken", "Another cashbox or bank account already has this code.");

    public static readonly Error AccountNotFound = Error.BusinessRule(
        "Accounting.CashAccount.AccountNotFound", "The ledger account does not exist.");

    public static readonly Error AccountInUse = Error.Conflict(
        "Accounting.CashAccount.AccountInUse", "Another cashbox or bank account is kept on this ledger account.");

    public static readonly Error BranchNotFound = Error.BusinessRule(
        "Accounting.CashAccount.BranchNotFound", "The branch does not exist.");

    public static readonly Error BranchNotActive = Error.BusinessRule(
        "Accounting.CashAccount.BranchNotActive", "The branch is inactive.");

    public static readonly Error HasBalance = Error.BusinessRule(
        "Accounting.CashAccount.HasBalance", "The cashbox or bank account still holds money; move it out before deactivating it.");
}
