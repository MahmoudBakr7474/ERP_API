namespace WarehouseERP.Purchasing.Application.Features.Suppliers.FindSimilarSuppliers;

/// <summary>Suppliers whose name is probably the same business, to warn before saving.</summary>
/// <param name="ExceptSupplierId">The supplier being edited, left out.</param>
public sealed record FindSimilarSuppliersQuery(string NameAr, string? NameEn, Guid? ExceptSupplierId);
