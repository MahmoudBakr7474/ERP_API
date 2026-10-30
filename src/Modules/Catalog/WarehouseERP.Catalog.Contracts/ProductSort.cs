namespace WarehouseERP.Catalog.Contracts;

/// <summary>What a product list or export is ordered by (ascending unless "descending" is asked for).</summary>
public enum ProductSort : byte
{
    /// <summary>The SKU (the default).</summary>
    Sku = 1,

    /// <summary>The Arabic name.</summary>
    NameAr = 2,

    /// <summary>The English name; products without one come first.</summary>
    NameEn = 3,

    /// <summary>When the product was created.</summary>
    CreatedAt = 4,
}
