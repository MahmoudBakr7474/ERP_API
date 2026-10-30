namespace WarehouseERP.Sales.Contracts;

/// <summary>A posted delivery or invoice, or an approved return, as Accounting discovers it.</summary>
public sealed record SalesPostedDocument(Guid Id, SalesDocumentType DocumentType, string Number, DateTime PostedAtUtc);
