using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.BankReconciliations;

// Reconciling banks is treasury work: accounting.treasury.manage (the endpoint) and company-wide access.
internal static class BankReconciliationAccess
{
    public static Result Check(ICurrentUser user)
    {
        var access = AccountingAccess.CompanyWide(user);
        return access.IsFailure ? access : AccountingAccess.Holds(user, AccountingPermissions.TreasuryManage);
    }
}
