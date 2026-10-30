# Generated documents: PDF, XLSX, CSV and imports

How WarehouseERP produces official files and reads uploaded tables. Decided in ADR 0021; Catalog is the first module
that uses it (`docs/modules/Catalog/IMPORT_EXPORT.md`). Every later module reuses the same foundation.

## Principles
- **The API generates every official file.** React only selects a report and its filters, previews, downloads and
  prints. Files are never built by scraping a rendered table, and React never recomputes a figure.
- **One query per report.** A report screen and its PDF/XLSX/CSV call the same Application read method with the same
  filters, so they always hold the same rows and totals. The file writers only lay values out.
- **The module owns its data, the foundation owns the layout.** Shared code (`BuildingBlocks.Infrastructure/Documents/`)
  knows nothing about products or invoices; modules know nothing about QuestPDF page geometry or CSV escaping.
- **No framework.** A `TabularReport` (columns, rows, filters, totals) covers lists; a document with its own layout
  (a product sheet, later an invoice) composes the shared page frame with QuestPDF directly.

## Libraries and licences
| Package | Version | Use | Licence |
|---|---|---|---|
| QuestPDF | 2026.9.1 | PDF layout (HarfBuzz shaping, bidi, pagination) | QuestPDF License: **Community only for eligible organisations** (annual gross revenue under USD 1M, not publicly traded or public sector; the client that owns the deployment counts, not the developer). Otherwise Professional or Enterprise. |
| ClosedXML | 0.105.1 | XLSX writing and reading | MIT (brings DocumentFormat.OpenXml MIT, ExcelNumberFormat MIT, RBush MIT, SixLabors.Fonts 1.0.0 Apache-2.0 as a transitive dependency) |
| CsvHelper | 33.1.0 | CSV writing and reading | MS-PL OR Apache-2.0 (34.x is a different API; not needed) |
| IBM Plex Sans Arabic | 2019 release (Google Fonts) | PDF font, Arabic + Latin in one family, as in the web app | SIL OFL 1.1, embedded in `BuildingBlocks.Infrastructure` (`Documents/Assets`, licence text beside it and copied to the output) |

`Documents:PdfLicense` is **required** (startup fails without it) so nobody generates PDFs under a licence they never
chose. `appsettings.Development.json` and the test host set `Community` (development and evaluation use is allowed).
**Before production the owner sets the licence the client organisation actually holds.** Open XML SDK is not used
directly; nothing needed streaming yet.

## Where things are
```
BuildingBlocks.Infrastructure/Documents/
  DocumentOptions, PdfLicense, DocumentRegistration   configuration (Documents section), AddErpDocuments()
  DocumentAssets                                       QuestPDF licence, font registration, logo (embedded or Documents:LogoPath)
  IDocumentIssuerSource, DocumentIssuer                the issuing company; Organization implements it (DocumentIssuerSource)
  DocumentContext                                      language, issuer, company-local generation time, logo; L(ar, en), Name(), ToLocal()
  DocumentFiles                                        RowLimit(format), ContextAsync(lang), Download(report|bytes) -> FileDownload
  TabularReport, ReportColumn, ColumnKind, ReportFilter
  BrandedPdf, PdfDocumentInfo, TabularPdfWriter        page frame + table
  XlsxReportWriter, CsvReportWriter, ReportValues      spreadsheets, CSV, value formatting
  TabularFileReader, ImportSheet, ImportRow, ImportLimits, ImportFileErrors   uploads
```

## Adding a report to a module (the Catalog pattern)
1. **Application**: a read-store method that takes the screen's filter and page; a screen query (paged) and an export
   query (`MaxRows`) in `Features/Reports` and `Features/Exports`. The export handler asks for one page of
   `MaxRows + 1` rows and returns `ExportErrors.TooManyRows` (422) when the extra row exists — never a silently
   truncated file.
2. **Presentation**: a `…Documents` class turns the rows into a `TabularReport` per language (column headers,
   localised enum labels, filters as printed); an endpoint
   `GET /api/<module>/exports/<report>?format=Xlsx|Csv|Pdf&lang=Ar|En&<filters>` with the report's read permission:
   validate `format`/`lang` (enums also bind from numbers), run the query with `files.RowLimit(format)`, get
   `files.ContextAsync(lang)`, return `DocumentFiles.Download(report, format, context)`.
3. **Frontend**: `ExportMenu` (`src/shared/ui/Files.tsx`) with the screen's filters; it lists only the formats the
   endpoint supports. A single document uses `PdfButton`.

## PDF conventions
- A4 portrait; landscape (`TabularReport.Landscape`) for wide tables. Margins 14 mm × 11 mm.
- Header on every page: issuing company (Organization's name in the document language, falling back to Arabic),
  document title, tax registration number / document number / date when present, logo at the end side, blue rule.
  First page only: the filters and summary figures (rows count first).
- Footer: generation time in the company's time zone (`yyyy-MM-dd HH:mm`), "WarehouseERP", "page X of Y".
- Tables: navy header row repeated on every page, rows never split, zebra shading, numbers right-aligned with Latin
  digits (`#,##0.########`, money two decimals), codes left to right. Readable in grayscale.
- **Arabic and mixed text.** QuestPDF lays each paragraph out as one bidi unit in its container's direction and does
  not keep digit groups joined by "-" together (`2026-10-30` would print as `30-10-2026`). Therefore:
  - every value is its own paragraph (`LabelValue`, table cells), never concatenated with an Arabic label;
  - `Directed(text)` gives a value the direction of its first letter (Arabic → RTL, anything else → LTR) and
    `AlignStart(context)` puts it on the document's reading side;
  - verify by glyph positions, not by eye: render the page and sort glyphs by x (PyMuPDF `rawdict`), as done for the
    Catalog files.
- `ThrowOnMissingTextGlyphs` is off and system fonts are not used: output is identical on every machine, and a name in
  a script the font lacks prints replacement glyphs instead of failing the document.
- Branding: the approved Codlek logo (vector, `docs/branding`) is the default; `Documents:LogoPath` replaces it with a
  company's own SVG. The company's names and tax number always come from Organization; nothing is invented.

## XLSX conventions
- Sheet 1 is the data: one header row (navy, white, bold), frozen, auto-filter over header and data (never the totals),
  then an optional bold totals row. Sheet 2 "Report info" holds title, company, tax number, generation time, row count,
  summary and filters.
- Numbers are numeric cells (`#,##0` counts, `#,##0.00` money, General for exact factors); codes are text cells with
  the `@` format (leading zeros kept); dates are date cells. Strings are set as values, never formulas.
- Arabic workbooks are right to left. Widths come from the content (capped at 60).

## CSV conventions
UTF-8 with BOM (Excel opens Arabic correctly), comma separator, CRLF, RFC 4180 quoting, invariant numbers without
separators, ISO dates, one header row in the file's language. Text starting with `= + - @ TAB CR` is prefixed with `'`
(OWASP CSV injection); numbers written as numbers are left alone. Excel users in comma-decimal locales should use the
XLSX file.

## Downloads
`FileDownload` answers `Content-Disposition: attachment` with `{slug}-{yyyyMMdd-HHmm}.{ext}` (an ASCII slug fixed by
the module, never user input), `Cache-Control: no-store`, `X-Content-Type-Options: nosniff`. Content types:
`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `text/csv; charset=utf-8`, `application/pdf`.
Limits: `Documents:MaxExportRows` (25 000) for XLSX/CSV, `Documents:MaxPdfRows` (5 000) for PDF.

## Reading uploads
`TabularFileReader.ReadAsync(IFormFile, ImportLimits)` reads `.xlsx` (every sheet) or `.csv` (one sheet, delimiter
detected, BOM honoured) into trimmed text rows numbered as in Excel. It never evaluates formulas (a formula cell gives
its cached value), never uses the uploaded name as a path, and refuses (400 `Validation.Failed`, field `file`):
`Import.File.Missing`, `UnsupportedType`, `TooLarge` (5 MB), `TooManyRows` (5 000 per sheet), `TooManyColumns` (40),
`Empty`, `Unreadable` (not a zip, corrupt, or a "zip bomb": more than 200 entries, 100 MB inflated, or a 1 MB+ entry
compressed over 200×). Endpoints add a `RequestSizeLimit` just above the file limit and `.DisableAntiforgery()`: the
Identity middleware already validates the XSRF header of every unsafe cookie request. What the rows mean, and every
business rule, is the importing module's (see Catalog).

## Not in this foundation
Background export jobs, report scheduling, a generic report builder, barcode label printing (next small enhancement:
it needs a barcode renderer such as ZXing.Net, Apache-2.0), and per-branch document branding.
