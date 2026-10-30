using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// The page every WarehouseERP PDF uses: A4 (portrait or landscape), the issuing company and the document title at
/// the start, the logo at the end, a blue rule, first-page details, and a footer with the generation time and
/// "page X of Y". Arabic documents are laid out right to left. Modules supply only the body.
/// </summary>
public static class BrandedPdf
{
    // Brand palette (docs/branding). Body text stays dark on white so documents print well in grayscale.
    public const string Navy = "#0B1B3F";
    public const string Primary = "#0B63CE";
    public const string Ink = "#0B1424";
    public const string Muted = "#5B6676";
    public const string Border = "#E2E7EE";
    public const string Shade = "#F5F7FA";

    public static byte[] Generate(DocumentContext context, PdfDocumentInfo info, Action<IContainer> body)
    {
        var document = Document.Create(doc => doc.Page(page =>
            {
                page.Size(info.Landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                page.MarginHorizontal(14, Unit.Millimetre);
                page.MarginVertical(11, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(s => s.FontFamily(DocumentAssets.FontFamily, DocumentAssets.FallbackFontFamily)
                    .FontSize(9).FontColor(Ink));
                if (context.IsArabic)
                    page.ContentFromRightToLeft();
                page.Header().Element(c => Header(c, context, info));
                page.Content().PaddingTop(8).Element(c => body(c));
                page.Footer().Element(c => Footer(c, context));
            }))
            .WithMetadata(new DocumentMetadata
            {
                Title = info.Title,
                Author = context.CompanyName ?? "WarehouseERP",
                Creator = "WarehouseERP",
                Producer = "WarehouseERP",
                Language = context.IsArabic ? "ar" : "en",
                CreationDate = context.GeneratedAt,
                ModifiedDate = context.GeneratedAt,
            });
        return document.GeneratePdf();
    }

    private static void Header(IContainer container, DocumentContext context, PdfDocumentInfo info) =>
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(title =>
                {
                    if (context.CompanyName is { } company)
                        title.Item().Value(context, company, x => x.FontSize(10.5f).SemiBold().FontColor(Navy));
                    title.Item().Text(t => t.AlignStart(context).Span(info.Title).FontSize(15).Bold());
                    var meta = MetaLine(context, info);
                    if (meta.Count > 0)
                        title.Item().PaddingTop(1).Inlined(line =>
                        {
                            line.Spacing(14);
                            foreach (var (label, value) in meta)
                                line.Item().Element(c => LabelValue(c, label, value, 8, bold: false));
                        });
                });
                row.ConstantItem(120).AlignMiddle().Height(29).Svg(context.LogoSvg).FitArea();
            });
            column.Item().PaddingTop(5).LineHorizontal(1.2f).LineColor(Primary);
            column.Item().ShowOnce().Element(c => FirstPageDetails(c, context, info));
        });

    /// <summary>"Label: value" as two paragraphs side by side, the value in its own direction (<see cref="Directed"/>).</summary>
    public static void LabelValue(IContainer container, string label, string value, float fontSize, bool bold) =>
        container.Row(row =>
        {
            row.Spacing(3);
            row.AutoItem().Text(label + ":").FontSize(fontSize).FontColor(Muted);
            row.AutoItem().Directed(value).Text(t =>
            {
                var span = t.Span(value).FontSize(fontSize);
                if (bold)
                    span.SemiBold();
            });
        });

    /// <summary>
    /// Gives a value its own paragraph direction, from its first letter: Arabic text reads right to left, anything else
    /// (codes, numbers, dates, English names) left to right. QuestPDF lays out each paragraph as one bidi unit in its
    /// container's direction and doesn't keep digit groups joined by "-" together, so "2026-10-30" would print as
    /// "30-10-2026" in an Arabic document. Pair it with <see cref="AlignStart"/> so the value still sits on the
    /// document's reading side.
    /// </summary>
    public static IContainer Directed(this IContainer container, string text) =>
        StartsRightToLeft(text) ? container.ContentFromRightToLeft() : container.ContentFromLeftToRight();

    /// <summary>Aligns text on the document's reading start (right in Arabic, left in English), whatever its own direction.</summary>
    public static TextDescriptor AlignStart(this TextDescriptor text, DocumentContext context)
    {
        if (context.IsArabic)
            text.AlignRight();
        else
            text.AlignLeft();
        return text;
    }

    /// <summary>A paragraph of one value, in its own direction, on the document's reading start.</summary>
    public static void Value(
        this IContainer container, DocumentContext context, string text, Func<TextSpanDescriptor, TextSpanDescriptor>? style = null) =>
        container.Directed(text).Text(t =>
        {
            t.AlignStart(context);
            style?.Invoke(t.Span(text));
        });

    /// <summary>Whether the first letter (Hebrew, Arabic and the scripts between them, or their presentation forms) reads right to left.</summary>
    public static bool StartsRightToLeft(string text)
    {
        foreach (var ch in text)
        {
            if (ch is >= '֐' and <= 'ࣿ' or >= 'יִ' and <= '﷿' or >= 'ﹰ' and <= 'ﻼ')
                return true;
            if (char.IsLetter(ch))
                return false;
        }
        return false;
    }

    private static List<(string Label, string Value)> MetaLine(DocumentContext context, PdfDocumentInfo info)
    {
        var parts = new List<(string, string)>();
        if (context.Issuer?.TaxRegistrationNumber is { } taxNumber)
            parts.Add((context.L("الرقم الضريبي", "Tax registration no."), taxNumber));
        if (info.DocumentNumber is { } number)
            parts.Add((context.L("رقم المستند", "Document no."), number));
        if (info.DocumentDate is { } date)
            parts.Add((context.L("التاريخ", "Date"), date));
        return parts;
    }

    private static void FirstPageDetails(IContainer container, DocumentContext context, PdfDocumentInfo info)
    {
        var details = info.Details ?? [];
        var summary = info.Summary ?? [];
        if (details.Count == 0 && summary.Count == 0)
            return;
        container.PaddingTop(6).Column(column =>
        {
            if (details.Count > 0)
                column.Item().Inlined(line =>
                {
                    line.Spacing(16);
                    line.VerticalSpacing(2);
                    foreach (var detail in details)
                        line.Item().Element(c => LabelValue(c, detail.Label, detail.Value, 8, bold: true));
                });
            if (summary.Count > 0)
                column.Item().PaddingTop(5).Inlined(boxes =>
                {
                    boxes.Spacing(6);
                    foreach (var figure in summary)
                        boxes.Item().Border(0.6f).BorderColor(Border).Background(Shade).PaddingVertical(3).PaddingHorizontal(7)
                            .Column(box =>
                            {
                                box.Item().Text(t => t.AlignStart(context).Span(figure.Label).FontSize(7).FontColor(Muted));
                                box.Item().Value(context, figure.Value, x => x.FontSize(10.5f).SemiBold().FontColor(Navy));
                            });
                });
        });
    }

    private static void Footer(IContainer container, DocumentContext context) =>
        container.Column(column =>
        {
            column.Item().LineHorizontal(0.5f).LineColor(Border);
            column.Item().PaddingTop(3).DefaultTextStyle(s => s.FontSize(7.5f).FontColor(Muted)).Row(row =>
            {
                row.RelativeItem().Row(generated =>
                {
                    generated.Spacing(3);
                    generated.AutoItem().Text(context.L("أُنشئ في", "Generated"));
                    generated.AutoItem().ContentFromLeftToRight()
                        .Text(context.GeneratedAt.ToString(ReportValues.DateTimeFormat, CultureInfo.InvariantCulture));
                    generated.AutoItem().Text("·  WarehouseERP");
                });
                row.AutoItem().Text(t =>
                {
                    t.Span(context.L("صفحة ", "Page "));
                    t.CurrentPageNumber();
                    t.Span(context.L(" من ", " of "));
                    t.TotalPages();
                });
            });
        });
}
