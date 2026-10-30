using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

public static class AllocationDomainErrors
{
    public static readonly Error PartyMismatch = Error.BusinessRule(
        "Accounting.Allocation.PartyMismatch", "Both open items belong to the same customer or supplier.");

    public static readonly Error AccountMismatch = Error.BusinessRule(
        "Accounting.Allocation.AccountMismatch", "Both open items are on the same control account.");

    public static readonly Error SidesInvalid = Error.BusinessRule(
        "Accounting.Allocation.SidesInvalid", "An allocation settles a debit item (an invoice, a refund owed) against a credit item (a receipt, a credit, a bill).");

    public static readonly Error AmountInvalid = Error.BusinessRule(
        "Accounting.Allocation.AmountInvalid", "The amount allocated is above zero.");

    public static readonly Error ExceedsOpen = Error.BusinessRule(
        "Accounting.Allocation.ExceedsOpen", "The amount allocated is more than one of the items still has open.");

    public static readonly Error AlreadyRemoved = Error.BusinessRule(
        "Accounting.Allocation.AlreadyRemoved", "The allocation was already removed.");
}
