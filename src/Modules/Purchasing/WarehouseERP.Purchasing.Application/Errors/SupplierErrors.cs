using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Errors;

// Application rules about suppliers; the supplier's own invariants are SupplierDomainErrors.
public static class SupplierErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Purchasing.Supplier.NotFound", "The supplier does not exist.");

    public static readonly Error CodeTaken = Error.Conflict(
        "Purchasing.Supplier.CodeTaken", "Another supplier has this code.");

    public static readonly Error TaxNumberTaken = Error.Conflict(
        "Purchasing.Supplier.TaxNumberTaken", "Another supplier of this country has this tax registration number.");

    public static readonly Error CommercialRegistrationTaken = Error.Conflict(
        "Purchasing.Supplier.CommercialRegistrationTaken",
        "Another supplier has this commercial registration number at this registry office.");

    public static readonly Error NotActive = Error.BusinessRule(
        "Purchasing.Supplier.NotActive", "The supplier is inactive: it takes no new orders.");
}
