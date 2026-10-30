using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Application.Features.Imports;

/// <summary>A Categories row after checking. <see cref="Action"/> is final once the planner's second pass ran.</summary>
public sealed record PlannedCategory(
    int Row, string? Code, string? NameAr, string? NameEn, string? ParentCode, Category? Existing, CatalogImportAction Action);
