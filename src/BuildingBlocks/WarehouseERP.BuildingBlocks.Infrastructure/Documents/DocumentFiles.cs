using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// Turns a module's report into a download: resolves the issuing company and its local time, writes the requested
/// format and answers with a safe file name. Modules call it from their endpoints after their own query has selected
/// and authorised the data.
/// </summary>
public sealed class DocumentFiles(
    IDocumentIssuerSource issuers, DocumentAssets assets, TimeProvider clock, IOptions<DocumentOptions> options)
{
    public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string CsvContentType = "text/csv; charset=utf-8";
    public const string PdfContentType = "application/pdf";

    /// <summary>Most rows a download in this format may hold; queries fetch one more to tell a larger result.</summary>
    public int RowLimit(ExportFormat format) =>
        format == ExportFormat.Pdf ? options.Value.MaxPdfRows : options.Value.MaxExportRows;

    public async Task<DocumentContext> ContextAsync(DocumentLanguage language, CancellationToken ct)
    {
        var issuer = await issuers.GetAsync(ct);
        return new DocumentContext(language, issuer, LocalNow(issuer), assets.LogoSvg);
    }

    /// <summary>Writes the report in the requested format and answers it as a download.</summary>
    public static IResult Download(TabularReport report, ExportFormat format, DocumentContext context) =>
        Download(Write(report, format, context), format, report.FileName, context);

    public static byte[] Write(TabularReport report, ExportFormat format, DocumentContext context) => format switch
    {
        ExportFormat.Xlsx => XlsxReportWriter.Write(report, context),
        ExportFormat.Csv => CsvReportWriter.Write(report, context),
        ExportFormat.Pdf => TabularPdfWriter.Write(report, context),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
    };

    /// <summary>
    /// A generated file as a download named <c>{fileName}-{yyyyMMdd-HHmm}.{ext}</c>. The base name is a fixed ASCII
    /// slug from the module, never user input, so it can't carry a path or a header injection.
    /// </summary>
    public static IResult Download(byte[] content, ExportFormat format, string fileName, DocumentContext context)
    {
        if (fileName.Length == 0 || !fileName.All(ch => char.IsAsciiLetterLower(ch) || char.IsAsciiDigit(ch) || ch == '-'))
            throw new ArgumentException("File names are lower-case ASCII slugs.", nameof(fileName));
        var (contentType, extension) = format switch
        {
            ExportFormat.Xlsx => (XlsxContentType, "xlsx"),
            ExportFormat.Csv => (CsvContentType, "csv"),
            _ => (PdfContentType, "pdf"),
        };
        var stamp = context.GeneratedAt.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        return new FileDownload(content, contentType, $"{fileName}-{stamp}.{extension}");
    }

    private DateTimeOffset LocalNow(DocumentIssuer? issuer)
    {
        var now = clock.GetUtcNow();
        if (issuer is null || !TimeZoneInfo.TryFindSystemTimeZoneById(issuer.TimeZoneId, out var zone))
            return now;
        return TimeZoneInfo.ConvertTime(now, zone);
    }
}
