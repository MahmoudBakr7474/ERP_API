namespace WarehouseERP.Purchasing.Contracts;

/// <summary>A supplier as other modules see it.</summary>
/// <param name="Id">Supplier id.</param>
/// <param name="Code">Unique supplier code.</param>
/// <param name="NameAr">Arabic name.</param>
/// <param name="NameEn">English name, when there is one.</param>
/// <param name="TaxRegistrationNumber">Tax registration number, when known.</param>
/// <param name="PaymentTermDays">Days after an invoice its payment falls due.</param>
/// <param name="IsActive">False once deactivated.</param>
public sealed record SupplierInfo(
    Guid Id, string Code, string NameAr, string? NameEn, string? TaxRegistrationNumber, int PaymentTermDays, bool IsActive);
