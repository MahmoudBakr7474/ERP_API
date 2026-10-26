namespace WarehouseERP.BuildingBlocks.Application.Security;

/// <summary>Claim names the API reads from the authenticated user.</summary>
public static class ErpClaimTypes
{
    /// <summary>The user id (a Guid).</summary>
    public const string Subject = "sub";

    /// <summary>The server-side session id.</summary>
    public const string SessionId = "sid";

    /// <summary>One claim per permission, e.g. <c>catalog.products.read</c>, or <see cref="AllPermissions"/>.</summary>
    public const string Permission = "permission";

    /// <summary>One claim per warehouse id the user may act on, or <see cref="AllWarehouses"/>.</summary>
    public const string Warehouse = "warehouse";

    /// <summary>Every permission (the Administrator role).</summary>
    public const string AllPermissions = "*";

    /// <summary>Every warehouse (company-wide access).</summary>
    public const string AllWarehouses = "*";
}

/// <summary>
/// A permission a module's endpoints require. Each module registers its own; together they are the catalog
/// roles are checked against and the host builds one authorization policy per name.
/// </summary>
public sealed record PermissionDefinition(string Name, string Module);
