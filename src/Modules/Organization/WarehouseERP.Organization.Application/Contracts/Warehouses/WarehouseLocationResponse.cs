namespace WarehouseERP.Organization.Application.Contracts.Warehouses;

/// <param name="RowVersion">Base64 row version; send it back with the next update.</param>
public sealed record WarehouseLocationResponse(
    Guid Id, string Code, string NameAr, string? NameEn, bool IsDefault, bool IsActive, string RowVersion);
