using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Errors;

// Application rules about locations; the location invariants themselves are WarehouseLocationDomainErrors.
public static class WarehouseLocationErrors
{
    public static readonly Error CodeTaken = Error.Conflict(
        "Organization.WarehouseLocation.CodeTaken", "Another location in this warehouse already uses this code.");

    public static readonly Error NotFound = Error.NotFound(
        "Organization.WarehouseLocation.NotFound", "The warehouse has no such location.");
}
