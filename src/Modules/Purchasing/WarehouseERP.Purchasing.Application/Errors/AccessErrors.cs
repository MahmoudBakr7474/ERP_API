using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Errors;

// Who may act on purchasing documents: their order's warehouse must be in the user's scope.
public static class AccessErrors
{
    public static readonly Error WarehouseDenied = Error.Forbidden(
        "Purchasing.Access.WarehouseDenied", "You are not allowed to access this warehouse's purchasing documents.");

    public static readonly Error CompanyWideRequired = Error.Forbidden(
        "Purchasing.Access.CompanyWideRequired", "Changing approval policies needs company-wide access.");
}
