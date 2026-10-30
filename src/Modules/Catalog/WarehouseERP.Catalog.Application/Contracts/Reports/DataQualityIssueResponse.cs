using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Contracts.Reports;

/// <summary>One data-quality issue of one product.</summary>
/// <param name="Detail">What the issue is about when it is not the product itself: a unit code, or "UNIT x factor".</param>
public sealed record DataQualityIssueResponse(
    Guid ProductId, string Sku, string NameAr, string? NameEn, bool ProductIsActive, CatalogDataIssue Issue, string? Detail);
