namespace WarehouseERP.Sales.Application.Features.Customers.FindSimilarCustomers;

/// <summary>Customers that are probably the same one (a similar name, or the same mobile number), to warn before saving.</summary>
/// <param name="ExceptCustomerId">The customer being edited, left out.</param>
public sealed record FindSimilarCustomersQuery(string NameAr, string? NameEn, string? Mobile, Guid? ExceptCustomerId);
