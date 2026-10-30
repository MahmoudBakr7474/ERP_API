namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>How a report column's values are written: decides cell types, number formats and alignment.</summary>
public enum ColumnKind
{
    /// <summary>Free text in the document's language (names); may mix Arabic and English.</summary>
    Text,

    /// <summary>An identifier that must stay text (SKU, barcode, code): leading zeros kept, read left to right.</summary>
    Code,

    /// <summary>A whole number such as a count of records.</summary>
    Count,

    /// <summary>An exact decimal such as a conversion factor or quantity: no forced decimals, no rounding.</summary>
    Quantity,

    /// <summary>A money amount: two decimals.</summary>
    Money,

    /// <summary>A date and time in the company's time zone.</summary>
    DateTime,
}
