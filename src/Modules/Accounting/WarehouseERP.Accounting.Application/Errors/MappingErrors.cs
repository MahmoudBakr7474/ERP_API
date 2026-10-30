using WarehouseERP.Accounting.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class MappingErrors
{
    public static readonly Error AccountNotFound = Error.BusinessRule(
        "Accounting.Mapping.AccountNotFound", "The account does not exist.");

    public static readonly Error NotMapped = Error.NotFound(
        "Accounting.Mapping.NotFound", "The posting role is not mapped.");

    public static Error RoleNotMapped(PostingRole role) => Error.BusinessRule(
        "Accounting.Mapping.RoleNotMapped", $"Map the posting role {role} to an account first; the system never picks an account by itself.");
}
