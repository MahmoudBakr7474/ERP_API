namespace WarehouseERP.Accounting.Domain;

/// <summary>A journal's final number and where it was taken from: its type's series in one fiscal year.</summary>
public sealed record DocumentNumber(string Number, Guid FiscalYearId, int SequenceNumber);
