using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Shared;

// Accounting V1 is company-wide (decision D17): every handler asks for company-wide access on top of the endpoint's
// permission. A journal's own type decides which permission edits it (manual journals or opening balances), so its
// handlers check that permission themselves.
internal static class AccountingAccess
{
    public static Result CompanyWide(ICurrentUser user) =>
        user.HasAllWarehouses ? Result.Success() : AccessErrors.CompanyWideRequired;

    public static Result Holds(ICurrentUser user, string permission) =>
        user.HasPermission(permission) ? Result.Success() : AccessErrors.PermissionDenied(permission);
}
