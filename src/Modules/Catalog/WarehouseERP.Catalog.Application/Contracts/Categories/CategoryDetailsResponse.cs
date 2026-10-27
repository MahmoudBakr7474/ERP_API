namespace WarehouseERP.Catalog.Application.Contracts.Categories;

/// <param name="RowVersion">Base64 row version; send it back with the next update.</param>
public sealed record CategoryDetailsResponse(
    Guid Id, Guid? ParentId, string Code, string NameAr, string? NameEn, bool IsActive, string RowVersion);
