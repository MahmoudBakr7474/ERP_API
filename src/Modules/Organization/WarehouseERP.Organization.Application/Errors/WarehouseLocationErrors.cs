using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Errors;

// Application rules about locations; the location invariants themselves are WarehouseLocationDomainErrors.
public static class WarehouseLocationErrors
{
    public static readonly Error CodeTaken = Error.Conflict(
        "Organization.WarehouseLocation.CodeTaken", "Another location in this warehouse already uses this code.");

    public static readonly Error NotFound = Error.NotFound(
        "Organization.WarehouseLocation.NotFound", "The warehouse has no such location.");

    public static readonly Error HoldsStock = Error.BusinessRule(
        "Organization.WarehouseLocation.HoldsStock", "The location still holds stock; move or adjust it out first.");

    public static readonly Error HasOpenInventoryDocuments = Error.BusinessRule(
        "Organization.WarehouseLocation.HasOpenInventoryDocuments", "Inventory documents waiting for approval would post into this location.");
}
