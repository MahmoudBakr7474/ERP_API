using System.Globalization;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// How values read in PDFs and CSVs. Digits are Latin (0-9) in both languages, as on the ERP's screens; numbers keep
/// their exact decimals (never rounded for display except money, which has two).
/// </summary>
public static class ReportValues
{
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm";

    /// <summary>Display text: thousands separators, Yes/No in the document's language.</summary>
    public static string Display(object? value, ColumnKind kind, DocumentLanguage language) => value switch
    {
        null => "",
        string s => s,
        bool b => language == DocumentLanguage.Ar ? (b ? "نعم" : "لا") : (b ? "Yes" : "No"),
        DateTime d => d.ToString(DateTimeFormat, CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString(DateTimeFormat, CultureInfo.InvariantCulture),
        _ when IsNumber(value) => FormatNumber(Convert.ToDecimal(value, CultureInfo.InvariantCulture), kind, grouped: true),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
    };

    /// <summary>Machine-readable text for CSV: invariant numbers without separators, ISO dates.</summary>
    public static string Raw(object? value, ColumnKind kind, DocumentLanguage language) => value switch
    {
        null => "",
        _ when IsNumber(value) => FormatNumber(Convert.ToDecimal(value, CultureInfo.InvariantCulture), kind, grouped: false),
        _ => Display(value, kind, language),
    };

    public static bool IsNumber(object? value) =>
        value is decimal or int or long or short or byte or double or float;

    private static string FormatNumber(decimal value, ColumnKind kind, bool grouped) => kind switch
    {
        // On screen and paper two decimals; in CSV (like the XLSX cell) the exact amount.
        ColumnKind.Money => value.ToString(grouped ? "#,##0.00" : "0.00######", CultureInfo.InvariantCulture),
        ColumnKind.Count => value.ToString(grouped ? "#,##0" : "0", CultureInfo.InvariantCulture),
        // decimal(18,8): up to eight decimals, none forced.
        _ => value.ToString(grouped ? "#,##0.########" : "0.########", CultureInfo.InvariantCulture),
    };
}
