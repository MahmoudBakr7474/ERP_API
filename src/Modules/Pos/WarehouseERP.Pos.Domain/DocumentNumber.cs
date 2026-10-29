namespace WarehouseERP.Pos.Domain;

/// <summary>A final number taken from its terminal's series inside the transaction that opens, sells or returns.</summary>
public sealed record DocumentNumber(string Number, short FiscalYear, int SequenceNumber);
