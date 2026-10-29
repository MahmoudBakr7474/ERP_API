using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Errors;

// Who may act on sales documents: their warehouse must be in the user's scope.
public static class AccessErrors
{
    public static readonly Error WarehouseDenied = Error.Forbidden(
        "Sales.Access.WarehouseDenied", "You are not allowed to access this warehouse's sales documents.");

    public static readonly Error CompanyWideRequired = Error.Forbidden(
        "Sales.Access.CompanyWideRequired", "Changing the sales settings needs company-wide access.");
}
