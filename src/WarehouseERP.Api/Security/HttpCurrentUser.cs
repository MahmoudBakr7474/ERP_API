using System.Security.Claims;
using WarehouseERP.BuildingBlocks.Application.Security;

namespace WarehouseERP.Api.Security;

/// <summary>Claim names the API reads, whatever identity provider issued them.</summary>
public static class ErpClaimTypes
{
    /// <summary>The user id (a Guid), as JWT "sub".</summary>
    public const string Subject = "sub";

    /// <summary>One claim per permission, e.g. <c>catalog.products.read</c>.</summary>
    public const string Permission = "permission";

    /// <summary>One claim per warehouse id the user may act on, or <see cref="AllWarehouses"/>.</summary>
    public const string Warehouse = "warehouse";

    public const string AllWarehouses = "*";
}

// The current HTTP user as the application sees it. Outside a request (no HttpContext) the user
// is anonymous: no permissions, no warehouses.
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ErpClaimTypes.Subject) ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool HasPermission(string permission) =>
        IsAuthenticated && Principal!.HasClaim(ErpClaimTypes.Permission, permission);

    public bool HasAllWarehouses =>
        IsAuthenticated && Principal!.HasClaim(ErpClaimTypes.Warehouse, ErpClaimTypes.AllWarehouses);

    public IReadOnlySet<Guid> WarehouseIds => !IsAuthenticated
        ? new HashSet<Guid>()
        : Principal!.FindAll(ErpClaimTypes.Warehouse)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
}
