namespace WarehouseERP.Sales.Contracts;

/// <summary>A customer as other modules see it.</summary>
/// <param name="Id">Customer id.</param>
/// <param name="Code">Unique customer code.</param>
/// <param name="NameAr">Arabic name.</param>
/// <param name="NameEn">English name, when there is one.</param>
/// <param name="CustomerType">Individual or business.</param>
/// <param name="TaxRegistrationNumber">Tax registration number, when known.</param>
/// <param name="Mobile">Mobile number, when known.</param>
/// <param name="IsActive">False once deactivated.</param>
public sealed record SalesCustomerInfo(
    Guid Id, string Code, string NameAr, string? NameEn, CustomerType CustomerType, string? TaxRegistrationNumber, string? Mobile,
    bool IsActive);
