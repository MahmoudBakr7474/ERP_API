namespace WarehouseERP.Catalog.Contracts;

/// <summary>What kind of code a barcode is; EAN-13 and GTIN values follow GS1 and carry a check digit.</summary>
public enum BarcodeType : byte
{
    /// <summary>The company's own code, printed on its labels; no GS1 rules.</summary>
    Internal = 1,

    /// <summary>A 13-digit European Article Number.</summary>
    Ean13 = 2,

    /// <summary>A GS1 Global Trade Item Number: 8, 12, 13 or 14 digits.</summary>
    Gtin = 3,

    /// <summary>Any other code (for example a supplier's); no GS1 rules.</summary>
    Other = 4,
}
