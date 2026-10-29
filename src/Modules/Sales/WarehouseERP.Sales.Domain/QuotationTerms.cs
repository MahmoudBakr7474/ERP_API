namespace WarehouseERP.Sales.Domain;

/// <summary>A quotation's commercial terms besides its lines.</summary>
/// <param name="ValidUntil">The last day the customer may accept it; not before the quotation date.</param>
/// <param name="PaymentTermDays">Days from invoice date to due date the order will carry, 0 to 365.</param>
public sealed record QuotationTerms(
    Guid CustomerId, DateOnly QuotationDate, DateOnly ValidUntil, string? CustomerReference, int PaymentTermDays, string? Notes);
