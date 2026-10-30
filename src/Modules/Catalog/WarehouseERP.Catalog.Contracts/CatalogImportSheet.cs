namespace WarehouseERP.Catalog.Contracts;

/// <summary>The kinds of rows a catalog import holds, in the order they are applied.</summary>
public enum CatalogImportSheet : byte
{
    /// <summary>Units of measure: code, names, whether quantities may be fractional.</summary>
    Units = 1,

    /// <summary>Categories: code, names, parent code.</summary>
    Categories = 2,

    /// <summary>Products: SKU, names, base unit, category, type, tracking.</summary>
    Products = 3,

    /// <summary>Additional units of products, with how many base units each holds.</summary>
    ProductUnits = 4,

    /// <summary>Barcodes printed on a product unit.</summary>
    Barcodes = 5,
}
