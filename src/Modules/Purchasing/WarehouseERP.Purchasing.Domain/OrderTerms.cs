namespace WarehouseERP.Purchasing.Domain;

/// <summary>What a draft order says besides its warehouse and lines.</summary>
/// <param name="SupplierId">An active supplier (checked by the application).</param>
/// <param name="SupplierReference">The supplier's quotation or reference.</param>
/// <param name="PaymentTermDays">Days from invoice date to due date, 0 to 365.</param>
public sealed record OrderTerms(
    Guid SupplierId, DateOnly? ExpectedDeliveryDate, string? SupplierReference, int PaymentTermDays, string? Notes);
