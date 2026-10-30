using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Contracts.Reports;

/// <summary>How many issues of one kind the report found (with the report's status and search filters).</summary>
public sealed record DataQualityIssueCount(CatalogDataIssue Issue, int Count);
