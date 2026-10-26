namespace WarehouseERP.Organization.Application.Contracts.Warehouses;

public sealed record WarehouseLocationResponse(Guid Id, string Code, string NameAr, string? NameEn, bool IsDefault, bool IsActive);
