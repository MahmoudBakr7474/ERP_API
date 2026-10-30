using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Contracts.Reports;

/// <summary>A warehouse of the organizational structure; its locations are listed by the location directory.</summary>
/// <param name="LocationCount">Locations matching the structure's status filter; <paramref name="ActiveLocationCount"/> of all of them active.</param>
public sealed record StructureWarehouseResponse(
    Guid Id, string Code, string NameAr, string? NameEn, WarehouseType Type, bool AllowsSelling, bool IsActive,
    int LocationCount, int ActiveLocationCount, string? DefaultLocationCode);
