using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Domain;

public static class WarehouseLocationDomainErrors
{
    public static readonly Error DefaultMustStayActive = Error.BusinessRule(
        "Organization.WarehouseLocation.DefaultMustStayActive", "A warehouse's default location can't be deactivated.");
}
