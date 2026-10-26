namespace WarehouseERP.Identity.Application.Features.Account.GetMe;

/// <param name="Permissions">Every permission the user holds (the Administrator gets the whole catalog).</param>
/// <param name="WarehouseIds">Effective warehouses; empty and <paramref name="IsCompanyWide"/> true means all.</param>
public sealed record MeResponse(
    Guid UserId, string UserName, string DisplayName, string? Email, IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions, bool IsCompanyWide, IReadOnlyList<Guid> WarehouseIds);
