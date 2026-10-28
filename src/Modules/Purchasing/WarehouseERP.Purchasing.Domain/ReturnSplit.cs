namespace WarehouseERP.Purchasing.Domain;

/// <summary>How a returned quantity divides between goods not yet invoiced and goods already invoiced.</summary>
/// <param name="UninvoicedQuantity">Returned before invoicing: it will not be invoiced.</param>
/// <param name="InvoicedQuantity">Already invoiced: the supplier owes a credit for it.</param>
/// <param name="CreditClaimNet">The credit's net amount, at the line's average invoiced net price.</param>
/// <param name="CreditClaimTax">The credit's tax, at the line's average invoiced tax.</param>
public sealed record ReturnSplit(decimal UninvoicedQuantity, decimal InvoicedQuantity, decimal CreditClaimNet, decimal CreditClaimTax);
