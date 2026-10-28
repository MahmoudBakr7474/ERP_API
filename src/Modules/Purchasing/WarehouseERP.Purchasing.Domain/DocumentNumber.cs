namespace WarehouseERP.Purchasing.Domain;

/// <summary>A final number taken from its series inside the transaction that approves or posts the document.</summary>
public sealed record DocumentNumber(string Number, short FiscalYear, int SequenceNumber);
