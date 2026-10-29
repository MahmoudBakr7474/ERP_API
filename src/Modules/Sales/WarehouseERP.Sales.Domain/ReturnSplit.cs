namespace WarehouseERP.Sales.Domain;

/// <summary>
/// How a returned quantity divides: the part not invoiced yet (it simply won't be invoiced) and the part already
/// invoiced, which the company owes back as a pending customer credit valued at the line's average invoiced net and tax.
/// </summary>
public sealed record ReturnSplit(decimal UninvoicedQuantity, decimal InvoicedQuantity, decimal CreditNetAmount, decimal CreditTaxAmount);
