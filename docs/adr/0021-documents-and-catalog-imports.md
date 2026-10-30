# ADR 0021 — Server-generated documents (PDF, XLSX, CSV), brand identity and the Catalog import

Status: accepted (2026-10-30), on `feature/catalog-operational-enhancements`. The owner's brief "Operational Excellence,
Document Generation, Reporting & Brand Identity" approved the architecture (the API generates every official file;
React selects, previews and downloads) and named the preferred libraries; this ADR records how it was implemented, with
Catalog as the first module.

## Context
The modules were functional but had no official documents: Accounting printed its screens from the browser and exported
CSV built in React; there was no XLSX, no branded PDF and no bulk master-data import. The owner supplied the Codlek
brand sheet (vector PDF with the logo and the "Navy Depth" palette) as the visual source of truth.

## Decision
1. **Shared foundation in `BuildingBlocks.Infrastructure/Documents`** (`docs/DOCUMENTS.md`): options, assets, the
   issuing company port, a tabular report model, the branded PDF page, XLSX/CSV writers, the upload reader, safe
   downloads. Modules keep their own queries and rules; there is no report builder, plugin model or job queue.
2. **Libraries.** QuestPDF 2026.9.1 (PDF; HarfBuzz shaping and bidi for Arabic), ClosedXML 0.105.1 (MIT), CsvHelper
   33.1.0 (MS-PL/Apache-2.0; the 34.x rewrite was not needed). Open XML SDK is only ClosedXML's dependency.
3. **QuestPDF licence is an explicit setting.** QuestPDF's Community licence covers only eligible organisations (the
   client owning the deployment: annual gross revenue under USD 1M, not publicly traded or public sector). The host
   requires `Documents:PdfLicense` (Community | Professional | Enterprise) and refuses to start without it.
   Development and tests set Community (evaluation use); **production must set the licence the owner holds**. The
   alternatives with permissive licences (PDFsharp/MigraDoc, raw SkiaSharp) have no Arabic shaping/bidi or would mean
   writing a layout engine.
4. **Issuer vs brand.** `IDocumentIssuerSource` (BuildingBlocks port, implemented by Organization) supplies the issuing
   company's names, tax number and time zone — never invented; the logo is the approved Codlek logo by default and
   `Documents:LogoPath` can replace it with a company's SVG. Generation times print in the company's time zone.
5. **Fonts.** IBM Plex Sans Arabic (SIL OFL 1.1, same family as the web app) is embedded; system fonts are not used and
   missing glyphs don't fail a document.
6. **Bidi.** Every value is its own paragraph with the direction of its first letter and the document's reading
   alignment; numbers are right-aligned with Latin digits. Verified on rendered pages by glyph positions.
7. **Exports** use the same Application read methods as the report screens, over the full filtered result, refused with
   422 (`<Module>.Export.TooManyRows`) above `Documents:MaxExportRows` / `MaxPdfRows` instead of truncating.
8. **Catalog import (two steps, all or nothing).** Preview plans every row against the current catalog and changes
   nothing; commit re-plans against the catalog as it is now and proceeds only if the outcome hashes to the previewed
   token (file fingerprint + mode + every row's action), then applies the plan through the domain methods and saves once.
   This gives revalidation, duplicate-submit protection and atomicity without an idempotency table or stored previews.
   Modes decide only what happens to existing keys (Create: error, Update: editable fields, Skip: untouched); fixed
   fields must match. Each row needs the permission of what it changes. Details: `docs/modules/Catalog/IMPORT_EXPORT.md`.
9. **Brand identity in the web app.** The extracted vector logo and the exact palette replace the old teal/amber tokens;
   derived tints are documented with their contrast (`docs/branding/README.md`). Semantic colours stay distinct.

## Consequences
- **No migration, no new table.** Audit history of imported rows is the temporal tables' `CreatedBy`/`UpdatedBy`, plus
  a structured log entry per commit.
- The OpenAPI contract changed additively: 15 Catalog operations (reports, exports, product sheet, import template,
  preview, commit, error report), `sort`/`descending` on `ListProducts`, 22 schemas.
- `Documents:PdfLicense` is a new required setting for every environment.
- New modules add their reports with the pattern in `docs/DOCUMENTS.md`; Accounting's browser-built CSV and printed
  reports can move to it when that module is next enhanced.

## Not done
Barcode label sheets (needs a barcode renderer), background exports, per-branch document branding, a dark theme.
