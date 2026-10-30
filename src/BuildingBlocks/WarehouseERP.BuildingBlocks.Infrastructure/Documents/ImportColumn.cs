namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>A column of an import sheet: its key (the row record's field), headers, whether it is required, help text.</summary>
public sealed record ImportColumn(
    string Key, string HeaderEn, string HeaderAr, bool Required, ImportColumnKind Kind, string HelpEn, string HelpAr,
    IReadOnlyList<ImportChoice>? Choices = null)
{
    public string Header(bool arabic) => (arabic ? HeaderAr : HeaderEn) + (Required ? " *" : "");
}
