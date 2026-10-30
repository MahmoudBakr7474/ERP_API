using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Allocations;

// Customers' items are allocated by whoever manages receivables, suppliers' by whoever manages payables; company-wide.
internal static class AllocationAccess
{
    public static Result CanAllocate(ICurrentUser user, PartyType partyType)
    {
        var access = AccountingAccess.CompanyWide(user);
        if (access.IsFailure)
            return access;
        return AccountingAccess.Holds(user, partyType == PartyType.Customer
            ? AccountingPermissions.ReceivablesManage
            : AccountingPermissions.PayablesManage);
    }

    /// <summary>The date an allocation settles from: the later of its two items' dates.</summary>
    public static DateOnly SettlesOn(Domain.OpenItem debit, Domain.OpenItem credit) =>
        debit.DocumentDate > credit.DocumentDate ? debit.DocumentDate : credit.DocumentDate;
}
