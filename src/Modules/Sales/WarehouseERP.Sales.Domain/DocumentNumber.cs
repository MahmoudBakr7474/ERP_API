namespace WarehouseERP.Sales.Domain;

/// <summary>A final number taken from its series inside the transaction that issues, confirms, posts or approves the document.</summary>
public sealed record DocumentNumber(string Number, short FiscalYear, int SequenceNumber);
