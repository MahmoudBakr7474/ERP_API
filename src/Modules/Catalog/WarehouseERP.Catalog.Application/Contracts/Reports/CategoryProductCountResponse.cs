namespace WarehouseERP.Catalog.Application.Contracts.Reports;

/// <summary>
/// A category with the products directly in it, listed in tree order (each parent before its children). The row
/// with a null <paramref name="Id"/> counts the products in no category.
/// </summary>
/// <param name="Level">0 for a top-level category, 1 below it, and so on.</param>
public sealed record CategoryProductCountResponse(
    Guid? Id, string? Code, string? NameAr, string? NameEn, Guid? ParentId, string? ParentCode, int Level, bool IsActive,
    int ActiveProducts, int InactiveProducts);
