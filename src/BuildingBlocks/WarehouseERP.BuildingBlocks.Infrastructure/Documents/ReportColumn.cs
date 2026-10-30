namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>A report column: its header in the document's language, its kind and its relative width in PDFs.</summary>
public sealed record ReportColumn(string Header, ColumnKind Kind = ColumnKind.Text, float Width = 1f);
