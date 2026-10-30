namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>A filter the report was produced with, already in the document's language ("Category: Beverages").</summary>
public sealed record ReportFilter(string Label, string Value);
