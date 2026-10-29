namespace WarehouseERP.Pos.Contracts;

/// <summary>How VAT applied to a sold line, as Sales' product tax classification said when it was sold.</summary>
public enum PosTaxTreatment : byte
{
    /// <summary>Taxed at a positive rate.</summary>
    Standard = 1,

    /// <summary>Taxable at a rate of zero.</summary>
    ZeroRated = 2,

    /// <summary>Outside VAT.</summary>
    Exempt = 3,
}
