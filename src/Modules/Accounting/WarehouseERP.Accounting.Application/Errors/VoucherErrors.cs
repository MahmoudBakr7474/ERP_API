using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class VoucherErrors
{
    public static readonly Error NotVoucher = Error.BusinessRule(
        "Accounting.Voucher.NotVoucher", "That journal type is not a voucher.");

    public static readonly Error CashAccountNotFound = Error.BusinessRule(
        "Accounting.Voucher.CashAccountNotFound", "The cashbox or bank account does not exist.");

    public static readonly Error CashAccountNotActive = Error.BusinessRule(
        "Accounting.Voucher.CashAccountNotActive", "The cashbox or bank account is inactive.");

    public static readonly Error PartyNotFound = Error.BusinessRule(
        "Accounting.Voucher.PartyNotFound", "The customer or supplier does not exist.");

    public static readonly Error BranchNotFound = Error.BusinessRule(
        "Accounting.Voucher.BranchNotFound", "The branch does not exist.");

    public static readonly Error BranchNotActive = Error.BusinessRule(
        "Accounting.Voucher.BranchNotActive", "The branch is inactive.");

    public static Error CounterAccountInvalid(int line) => Error.BusinessRule(
        "Accounting.Voucher.CounterAccountInvalid",
        $"Counter line {line}: use an active posting account open to manual postings that is neither a customer or supplier account (use a receipt, payment or refund) nor a cashbox or bank (use a transfer).");

    public static Error AllocationItemNotFound(int line) => Error.BusinessRule(
        "Accounting.Voucher.AllocationItemNotFound", $"Allocation {line}: the open item does not exist.");

    public static Error AllocationItemMismatch(int line) => Error.BusinessRule(
        "Accounting.Voucher.AllocationItemMismatch",
        $"Allocation {line}: the open item belongs to another party, another control account, or sits on the same side as the voucher.");

    public static Error AllocationExceedsOpen(int line) => Error.BusinessRule(
        "Accounting.Voucher.AllocationExceedsOpen", $"Allocation {line}: the open item has less than that still open.");

    public static readonly Error BillNumberTaken = Error.Conflict(
        "Accounting.Voucher.BillNumberTaken", "This supplier already has an expense bill with that number.");
}
