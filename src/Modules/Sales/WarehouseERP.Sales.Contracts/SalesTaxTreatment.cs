namespace WarehouseERP.Sales.Contracts;

/// <summary>How VAT applies to a sales line. Anything else is refused, never treated as one of these.</summary>
public enum SalesTaxTreatment : byte
{
    /// <summary>Taxed at a positive rate: the company's default VAT rate unless the line names another.</summary>
    Standard = 1,

    /// <summary>Taxable at a rate of zero (for example exports); the rate is always 0.</summary>
    ZeroRated = 2,

    /// <summary>Outside VAT; no tax and the rate is always 0.</summary>
    Exempt = 3,
}
