namespace WarehouseERP.Catalog.Contracts;

/// <summary>A unit a product is counted in, as other modules see it.</summary>
/// <param name="Id">Product unit id.</param>
/// <param name="ProductId">The product.</param>
/// <param name="UnitId">The unit of measure.</param>
/// <param name="UnitCode">The unit's code.</param>
/// <param name="UnitNameAr">The unit's Arabic name.</param>
/// <param name="ConversionFactor">How many base units one of this unit holds; 1 for the base unit. Fixed once created.</param>
/// <param name="IsBase">True for the unit the product's stock is kept in (Product.BaseUnitId).</param>
/// <param name="UnitAllowsFraction">Whether a quantity in this unit may have decimals.</param>
/// <param name="BaseUnitAllowsFraction">Whether a quantity in the product's base unit may have decimals.</param>
/// <param name="IsActive">True only while both the product unit and its unit are active.</param>
public sealed record ProductUnitInfo(
    Guid Id, Guid ProductId, Guid UnitId, string UnitCode, string UnitNameAr, decimal ConversionFactor, bool IsBase,
    bool UnitAllowsFraction, bool BaseUnitAllowsFraction, bool IsActive);
