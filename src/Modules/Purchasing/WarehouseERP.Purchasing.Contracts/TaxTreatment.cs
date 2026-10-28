namespace WarehouseERP.Purchasing.Contracts;

/// <summary>How a line's tax is treated in cost.</summary>
public enum TaxTreatment : byte
{
    /// <summary>Input tax the company recovers: recorded on the document, never part of inventory cost.</summary>
    Recoverable = 1,

    /// <summary>
    /// Tax the company cannot recover, which would belong in inventory cost. Not supported in V1: a line that declares it
    /// is refused, never silently treated as recoverable.
    /// </summary>
    NonRecoverable = 2,
}
