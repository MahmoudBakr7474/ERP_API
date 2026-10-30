using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Organization.Application.Contracts.Companies;

namespace WarehouseERP.Organization.Presentation.Documents;

// The company profile on A4 portrait: the company's own identification, address, contact and settings, as recorded in
// Organization. A detail that was never recorded is left out, never printed as an empty or invented value; a section
// with nothing recorded says so. The company code is the document number.
internal static class CompanyProfileDocument
{
    public static byte[] Write(DocumentContext c, CompanyResponse company)
    {
        var info = new PdfDocumentInfo(c.L("ملف الشركة", "Company profile"), DocumentNumber: company.Code);
        var city = company.City;
        return BrandedPdf.Generate(c, info, body => body.Column(column =>
        {
            column.Spacing(12);
            column.Item().Column(heading =>
            {
                heading.Item().Value(c, company.NameAr, x => x.FontSize(15).Bold().FontColor(BrandedPdf.Navy));
                if (company.NameEn is { } nameEn)
                    heading.Item().Value(c, nameEn, x => x.FontSize(11).FontColor(BrandedPdf.Muted));
            });
            column.Item().Element(x => Section(x, c, c.L("بيانات التعريف", "Identification"),
            [
                (c.L("كود الشركة", "Company code"), company.Code),
                (c.L("الاسم القانوني (عربي)", "Legal name (Arabic)"), company.NameAr),
                (c.L("الاسم القانوني (إنجليزي)", "Legal name (English)"), company.NameEn),
                (c.L("الرقم الضريبي", "Tax registration no."), company.TaxRegistrationNumber),
                (c.L("رقم السجل التجاري", "Commercial registration no."), company.CommercialRegistrationNumber),
                (c.L("مكتب السجل التجاري", "Commercial registry"), company.CommercialRegistry),
            ]));
            column.Item().Element(x => Section(x, c, c.L("العنوان والاتصال", "Address and contact"),
            [
                (c.L("العنوان", "Address"), company.AddressLine),
                (c.L("المدينة", "City"), city is null ? null : c.Name(city.NameAr, city.NameEn)),
                (c.L("المحافظة", "Governorate"), city is null ? null : c.Name(city.GovernorateNameAr, city.GovernorateNameEn)),
                (c.L("الدولة", "Country"), city is null ? null : c.Name(city.CountryNameAr, city.CountryNameEn)),
                (c.L("الهاتف", "Phone"), company.Phone),
                (c.L("البريد الإلكتروني", "E-mail"), company.Email),
            ]));
            column.Item().Element(x => Section(x, c, c.L("الإعدادات", "Settings"),
            [
                (c.L("العملة الأساسية", "Base currency"), company.BaseCurrencyCode),
                (c.L("المنطقة الزمنية", "Time zone"), company.TimeZoneId),
            ]));
        }));
    }

    private static void Section(IContainer container, DocumentContext c, string title, IReadOnlyList<(string Label, string? Value)> fields)
    {
        var recorded = fields.Where(f => !string.IsNullOrWhiteSpace(f.Value)).ToList();
        container.Column(column =>
        {
            column.Item().PaddingTop(4).BorderBottom(1).BorderColor(BrandedPdf.Primary).PaddingBottom(3)
                .Text(t => t.AlignStart(c).Span(title).FontSize(11).SemiBold().FontColor(BrandedPdf.Navy));
            if (recorded.Count == 0)
            {
                column.Item().PaddingTop(6).Text(t => t.AlignStart(c).Span(c.L("لم تُسجَّل بيانات بعد.", "Nothing recorded yet."))
                    .FontSize(9).FontColor(BrandedPdf.Muted));
                return;
            }
            column.Item().PaddingTop(4).Border(0.6f).BorderColor(BrandedPdf.Border).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(3f);
                });
                foreach (var (label, value) in recorded)
                {
                    table.Cell().Background(BrandedPdf.Shade).BorderBottom(0.5f).BorderColor(BrandedPdf.Border).Padding(5)
                        .Text(t => t.AlignStart(c).Span(label).FontSize(8.5f).FontColor(BrandedPdf.Muted));
                    table.Cell().BorderBottom(0.5f).BorderColor(BrandedPdf.Border).Padding(5)
                        .Value(c, value!, x => x.FontSize(9.5f).SemiBold());
                }
            });
        });
    }
}
