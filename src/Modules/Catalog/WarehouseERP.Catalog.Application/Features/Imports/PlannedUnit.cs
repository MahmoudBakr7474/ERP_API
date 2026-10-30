using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Application.Features.Imports;

/// <summary>A Units row after checking: its values (null where invalid), the unit it matches, and what will happen.</summary>
public sealed record PlannedUnit(
    int Row, string? Code, string? NameAr, string? NameEn, bool AllowsFraction, Unit? Existing, CatalogImportAction Action);
