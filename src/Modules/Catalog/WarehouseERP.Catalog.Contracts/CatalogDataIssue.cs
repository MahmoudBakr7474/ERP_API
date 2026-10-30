namespace WarehouseERP.Catalog.Contracts;

/// <summary>A gap or inconsistency in a product's master data, found by the catalog data-quality report.</summary>
public enum CatalogDataIssue : byte
{
    /// <summary>A stock or non-stock item with no active barcode on an active unit (services are not expected to have one).</summary>
    NoBarcode = 1,

    /// <summary>The product is in no category.</summary>
    NoCategory = 2,

    /// <summary>The product stays in a category that was retired.</summary>
    InactiveCategory = 3,

    /// <summary>The product's base unit (the unit its stock is kept in) was retired.</summary>
    InactiveBaseUnit = 4,

    /// <summary>An active product unit points to a retired unit of measure.</summary>
    InactiveUnitInUse = 5,

    /// <summary>A unit holds a fraction of a base unit that no longer allows fractions (its setting changed later).</summary>
    FractionalFactorOnWholeUnit = 6,

    /// <summary>No English name, so English documents fall back to the Arabic one.</summary>
    MissingEnglishName = 7,
}
