using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

// Accounting V1 is for company-wide users only (decision D17); branch figures are report filters, not security.
public static class AccessErrors
{
    public static readonly Error CompanyWideRequired = Error.Forbidden(
        "Accounting.Access.CompanyWideRequired", "Accounting needs company-wide access.");

    public static Error PermissionDenied(string permission) => Error.Forbidden(
        "Accounting.Access.PermissionDenied", $"You need the {permission} permission.");
}
