namespace WarehouseERP.Purchasing.Contracts;

/// <summary>A posted goods receipt, supplier invoice or supplier return as Accounting discovers it.</summary>
public sealed record PurchasingPostedDocument(Guid Id, PurchasingDocumentType DocumentType, string Number, DateTime PostedAtUtc);
