using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.Imports;

/// <summary>
/// What the importing user may do, from the permissions the single-location endpoints require: new locations need
/// locations.create, renames locations.update. Warehouse scope is checked per row.
/// </summary>
public sealed record LocationImportPermissions(bool Create, bool Update)
{
    public bool Any => Create || Update;

    public static LocationImportPermissions Of(ICurrentUser user) => new(
        user.HasPermission(OrganizationPermissions.LocationsCreate), user.HasPermission(OrganizationPermissions.LocationsUpdate));
}
