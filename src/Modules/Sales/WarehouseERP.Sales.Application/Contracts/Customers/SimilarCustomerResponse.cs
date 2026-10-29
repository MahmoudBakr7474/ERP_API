namespace WarehouseERP.Sales.Application.Contracts.Customers;

/// <summary>A customer that is probably the same one (a similar name or the same mobile): a warning, never a refusal.</summary>
public sealed record SimilarCustomerResponse(
    Guid Id, string Code, string NameAr, string? NameEn, string? Mobile, string? TaxRegistrationNumber, bool IsActive);
