namespace WarehouseERP.Purchasing.Domain;

/// <summary>An invoice line as entered: the order line it bills, the quantity in that line's unit and the supplier's price.</summary>
public sealed record InvoiceLineInput(Guid OrderLineId, decimal Quantity, LinePrice Price);
