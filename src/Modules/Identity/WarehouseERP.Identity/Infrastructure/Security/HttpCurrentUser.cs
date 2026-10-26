using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WarehouseERP.BuildingBlocks.Application.Security;

namespace WarehouseERP.Identity.Infrastructure.Security;

// The current HTTP user as every module's handlers see it, read from the claims the session handler issued.
// Outside a request (no HttpContext) the user is anonymous: no permissions, no warehouses.
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? UserId =>
        IsAuthenticated && Guid.TryParse(Principal!.FindFirstValue(ErpClaimTypes.Subject), out var id) ? id : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool HasPermission(string permission) =>
        IsAuthenticated && (Principal!.HasClaim(ErpClaimTypes.Permission, permission)
                            || Principal.HasClaim(ErpClaimTypes.Permission, ErpClaimTypes.AllPermissions));

    public bool HasAllWarehouses =>
        IsAuthenticated && Principal!.HasClaim(ErpClaimTypes.Warehouse, ErpClaimTypes.AllWarehouses);

    public IReadOnlySet<Guid> WarehouseIds => !IsAuthenticated
        ? new HashSet<Guid>()
        : Principal!.FindAll(ErpClaimTypes.Warehouse)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
}
