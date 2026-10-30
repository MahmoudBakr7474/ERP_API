namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// Reads an uploaded sheet's header row against a module's import columns. Headers are matched in either language (and
/// by the field key), ignoring case, spaces, the "*" required marker and _ - ( ) tatweel; choices accept the API name
/// or either label. What the values mean stays the importing module's.
/// </summary>
public static class ImportColumns
{
    /// <summary>The column a header names, or null.</summary>
    public static ImportColumn? Find(IReadOnlyList<ImportColumn> columns, string header)
    {
        var name = Normalize(header);
        return columns.FirstOrDefault(c => Normalize(c.Key) == name || Normalize(c.HeaderEn) == name || Normalize(c.HeaderAr) == name);
    }

    /// <summary>Column key → cell index for the sheet's headers (the first header of a column wins), and the required
    /// columns the sheet lacks.</summary>
    public static (Dictionary<string, int> Indexes, IReadOnlyList<ImportColumn> Missing) Map(
        IReadOnlyList<ImportColumn> columns, IReadOnlyList<string> headers)
    {
        var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < headers.Count; i++)
            if (Find(columns, headers[i]) is { } column)
                indexes.TryAdd(column.Key, i);
        return (indexes, columns.Where(c => c.Required && !indexes.ContainsKey(c.Key)).ToList());
    }

    /// <summary>A cell as the import rows expect it: a choice's label becomes its API name; anything else is unchanged.</summary>
    public static string? Value(ImportColumn column, string? cell)
    {
        if (cell is null || column.Choices is null)
            return cell;
        var text = Normalize(cell);
        return column.Choices.FirstOrDefault(c => Normalize(c.Value) == text || Normalize(c.LabelEn) == text || Normalize(c.LabelAr) == text)
            ?.Value ?? cell;
    }

    public static string Normalize(string text) =>
        new(text.Where(ch => !char.IsWhiteSpace(ch) && ch is not ('*' or '_' or '-' or '(' or ')' or 'ـ')).Select(char.ToLowerInvariant).ToArray());
}
