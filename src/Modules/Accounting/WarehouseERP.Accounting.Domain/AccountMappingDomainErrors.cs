using WarehouseERP.Accounting.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

public static class AccountMappingDomainErrors
{
    public static readonly Error RoleInvalid = Error.BusinessRule(
        "Accounting.Mapping.RoleInvalid", "The posting role is unknown.");

    public static readonly Error NotPostingAccount = Error.BusinessRule(
        "Accounting.Mapping.NotPostingAccount", "A posting role is mapped to a posting account, not a header.");

    public static readonly Error AccountNotActive = Error.BusinessRule(
        "Accounting.Mapping.AccountNotActive", "A posting role can't be mapped to an inactive account.");

    public static Error AccountDoesNotFitRole(PostingRole role) => Error.BusinessRule(
        "Accounting.Mapping.AccountDoesNotFitRole",
        $"The account does not fit the role {role}: it needs a {string.Join(" or ", PostingRoleRules.TypesOf(role))} account"
        + (PostingRoleRules.PartyOf(role) is { } party ? $" that names a {party} on each line." : " without a party."));
}
