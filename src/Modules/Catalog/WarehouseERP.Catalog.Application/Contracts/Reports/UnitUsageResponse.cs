namespace WarehouseERP.Catalog.Application.Contracts.Reports;

/// <summary>A unit of measure and how widely products use it.</summary>
/// <param name="BaseUnitProducts">Products that keep their stock in this unit.</param>
/// <param name="ProductUnits">Product units in this unit (base units included), active or not.</param>
public sealed record UnitUsageResponse(
    Guid Id, string Code, string NameAr, string? NameEn, bool AllowsFraction, bool IsActive, int BaseUnitProducts, int ProductUnits);
