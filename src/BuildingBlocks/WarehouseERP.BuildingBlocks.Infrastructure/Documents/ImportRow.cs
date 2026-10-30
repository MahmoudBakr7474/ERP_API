namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>A data row as text, numbered as the user sees it in Excel (the header is row 1).</summary>
/// <param name="Cells">One trimmed value per header; null for an empty cell.</param>
public sealed record ImportRow(int Number, IReadOnlyList<string?> Cells);
