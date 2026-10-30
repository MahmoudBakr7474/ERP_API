using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Application.Features.Imports;

/// <summary>A checked row: what to do and with which tracked warehouse or location.</summary>
public sealed record PlannedLocation(
    int Row, LocationImportAction Action, Warehouse? Warehouse, WarehouseLocation? Existing, string? Code, string? NameAr, string? NameEn);
