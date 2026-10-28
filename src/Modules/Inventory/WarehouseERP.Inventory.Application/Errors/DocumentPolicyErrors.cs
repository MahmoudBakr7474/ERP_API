using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Errors;

public static class DocumentPolicyErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Inventory.DocumentPolicy.NotFound", "This document type has no approval policy.");

    public static readonly Error CompanyWideAccessRequired = Error.Forbidden(
        "Inventory.DocumentPolicy.CompanyWideAccessRequired", "Only a user with company-wide access can change approval policies.");
}
