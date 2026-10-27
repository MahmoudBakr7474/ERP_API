namespace WarehouseERP.Catalog.Application.Contracts.Categories;

public sealed record CategoryResponse(Guid Id, Guid? ParentId, string Code, string NameAr, string? NameEn, bool IsActive);
