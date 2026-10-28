namespace WarehouseERP.Purchasing.Application.Contracts.Suppliers;

/// <summary>A supplier whose name is probably the same business: a warning, not a refusal.</summary>
public sealed record SimilarSupplierResponse(Guid Id, string Code, string NameAr, string? NameEn, string? TaxRegistrationNumber, bool IsActive);
