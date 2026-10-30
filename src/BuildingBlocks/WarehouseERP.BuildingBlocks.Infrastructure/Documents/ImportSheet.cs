namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>One sheet (or the CSV file) of an uploaded table: its header row and its non-empty data rows.</summary>
/// <param name="Headers">The first row's cells, trimmed.</param>
public sealed record ImportSheet(string Name, IReadOnlyList<string> Headers, IReadOnlyList<ImportRow> Rows);
