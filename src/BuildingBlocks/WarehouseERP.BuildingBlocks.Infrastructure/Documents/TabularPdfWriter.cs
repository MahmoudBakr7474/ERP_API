using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// A <see cref="TabularReport"/> as a branded PDF: the column headings repeat on every page, rows are never split,
/// numbers are right-aligned in both directions (their digits always read left to right) and codes keep their order.
/// </summary>
public static class TabularPdfWriter
{
    public static byte[] Write(TabularReport report, DocumentContext context) =>
        BrandedPdf.Generate(
            context,
            new PdfDocumentInfo(report.Title, report.Filters, WithRowCount(report, context), Landscape: report.Landscape),
            body => Table(body, report, context));

    /// <summary>The table alone, for documents that put one inside a larger layout.</summary>
    public static void Table(IContainer container, TabularReport report, DocumentContext context) =>
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var column in report.Columns)
                    columns.RelativeColumn(column.Width);
            });
            table.Header(header =>
            {
                foreach (var column in report.Columns)
                    header.Cell().Background(BrandedPdf.Navy).PaddingVertical(4).PaddingHorizontal(4)
                        .Text(t =>
                        {
                            Align(t, column.Kind, context);
                            t.Span(column.Header).FontSize(8).SemiBold().FontColor(Colors.White);
                        });
            });

            if (report.Rows.Count == 0)
            {
                table.Cell().ColumnSpan((uint)report.Columns.Count).PaddingVertical(10).AlignCenter()
                    .Text(context.L("لا توجد سجلات مطابقة.", "No matching records.")).FontColor(BrandedPdf.Muted);
                return;
            }

            for (var r = 0; r < report.Rows.Count; r++)
                for (var c = 0; c < report.Columns.Count; c++)
                    Cell(table.Cell(), report.Rows[r][c], report.Columns[c], context, shaded: r % 2 == 1, total: false);

            if (report.Totals is { } totals)
                for (var c = 0; c < report.Columns.Count; c++)
                    Cell(table.Cell().BorderTop(1).BorderColor(BrandedPdf.Navy), totals[c], report.Columns[c], context,
                        shaded: false, total: true);
        });

    private static IReadOnlyList<ReportFilter> WithRowCount(TabularReport report, DocumentContext context) =>
        [new(context.L("عدد السجلات", "Rows"), ReportValues.Display(report.Rows.Count, ColumnKind.Count, context.Language)),
            .. report.Summary ?? []];

    private static void Cell(IContainer cell, object? value, ReportColumn column, DocumentContext context, bool shaded, bool total)
    {
        var text = ReportValues.Display(value, column.Kind, context.Language);
        // Codes, numbers, dates and English names get a left-to-right paragraph; Arabic text keeps the page's.
        var box = cell.ShowEntire().Background(shaded ? BrandedPdf.Shade : Colors.White)
            .BorderBottom(0.5f).BorderColor(BrandedPdf.Border).PaddingVertical(3).PaddingHorizontal(4);
        (column.Kind == ColumnKind.Text ? box.Directed(text) : box.ContentFromLeftToRight()).Text(t =>
        {
            Align(t, column.Kind, context);
            var span = t.Span(text).FontSize(8.5f);
            if (total)
                span.SemiBold();
        });
    }

    // Physical alignment, so it holds inside left-to-right paragraphs too: numbers always on the right (their digits
    // line up), everything else on the reading start side.
    private static void Align(TextDescriptor text, ColumnKind kind, DocumentContext context)
    {
        if (kind is ColumnKind.Count or ColumnKind.Quantity or ColumnKind.Money || context.IsArabic)
            text.AlignRight();
        else
            text.AlignLeft();
    }
}
