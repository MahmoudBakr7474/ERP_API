using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Catalog.Application.Contracts.Imports;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.Catalog.Application.Features.Exports.ExportCategories;
using WarehouseERP.Catalog.Application.Features.Exports.ExportUnits;
using WarehouseERP.Catalog.Application.Features.Imports.CommitCatalogImport;
using WarehouseERP.Catalog.Application.Features.Imports.PreviewCatalogImport;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Presentation.Imports;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Presentation;

// Master-data import in two steps (docs/modules/Catalog/IMPORT_EXPORT.md): preview shows what each row would do and
// every problem, changing nothing; commit sends the same file, mode and the preview's token, and imports everything
// or nothing. Reading the catalog opens these endpoints; what a row may create or change is checked per row against
// the user's catalog permissions. Uploads are capped below Kestrel's default; the Identity middleware has already
// checked the anti-forgery header of a cookie session, so the form binding's own check is turned off.
internal static class CatalogImportEndpoints
{
    public const string Route = "/api/catalog/imports";

    // The file limit plus room for the multipart envelope and the small form fields.
    private static readonly long MaxRequestBytes = CatalogImportFile.Limits.MaxFileBytes + 64 * 1024;

    public static IEndpointRouteBuilder MapCatalogImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("CatalogImports").RequireAuthorization(CatalogPermissions.ProductsRead);

        group.MapGet("/template", TemplateAsync)
            .WithName("GetCatalogImportTemplate")
            .Produces(StatusCodes.Status200OK, contentType: DocumentFiles.XlsxContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/preview", PreviewAsync)
            .WithName("PreviewCatalogImport")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .Produces<CatalogImportPreviewResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/commit", CommitAsync)
            .WithName("CommitCatalogImport")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .Produces<CatalogImportResultResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/error-report", ErrorReportAsync)
            .WithName("GetCatalogImportErrorReport")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .Produces(StatusCodes.Status200OK, contentType: DocumentFiles.XlsxContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> TemplateAsync(
        IQueryHandler<ExportUnitsQuery, IReadOnlyList<UnitUsageResponse>> units,
        IQueryHandler<ExportCategoriesQuery, IReadOnlyList<CategoryProductCountResponse>> categories,
        DocumentFiles files, CancellationToken ct, DocumentLanguage lang = DocumentLanguage.Ar)
    {
        if (!Enum.IsDefined(lang))
            return Error.Validation([new ValidationError("lang", "Export.Language.Invalid", "Choose Ar or En.")]).ToProblem();
        var activeUnits = await units.HandleAsync(new ExportUnitsQuery(true, int.MaxValue - 1), ct);
        var activeCategories = await categories.HandleAsync(new ExportCategoriesQuery(true, int.MaxValue - 1), ct);
        if (activeUnits.IsFailure)
            return activeUnits.Error.ToProblem();
        if (activeCategories.IsFailure)
            return activeCategories.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        return DocumentFiles.Download(
            CatalogImportTemplate.Write(c, activeUnits.Value, activeCategories.Value), ExportFormat.Xlsx, "catalog-import-template", c);
    }

    private static async Task<IResult> PreviewAsync(
        IFormFile? file, [FromForm] CatalogImportMode? mode, [FromForm] CatalogImportSheet? sheet,
        IQueryHandler<PreviewCatalogImportQuery, CatalogImportPreviewResponse> handler, CancellationToken ct)
    {
        var upload = await UploadAsync(file, mode, sheet, ct);
        if (upload.IsFailure)
            return upload.Error.ToProblem();
        return (await handler.HandleAsync(new PreviewCatalogImportQuery(upload.Value.Batch, mode!.Value, upload.Value.Fingerprint), ct))
            .ToHttpResult(TypedResults.Ok);
    }

    private static async Task<IResult> CommitAsync(
        IFormFile? file, [FromForm] CatalogImportMode? mode, [FromForm] CatalogImportSheet? sheet, [FromForm] string? previewToken,
        ICommandHandler<CommitCatalogImportCommand, CatalogImportResultResponse> handler, CancellationToken ct)
    {
        var upload = await UploadAsync(file, mode, sheet, ct);
        if (upload.IsFailure)
            return upload.Error.ToProblem();
        return (await handler.HandleAsync(
                new CommitCatalogImportCommand(upload.Value.Batch, mode!.Value, upload.Value.Fingerprint, previewToken ?? ""), ct))
            .ToHttpResult(TypedResults.Ok);
    }

    // Every problem of the preview, in a workbook the user can sort and fix from (the screen shows the first 1,000).
    private static async Task<IResult> ErrorReportAsync(
        IFormFile? file, [FromForm] CatalogImportMode? mode, [FromForm] CatalogImportSheet? sheet,
        IQueryHandler<PreviewCatalogImportQuery, CatalogImportPreviewResponse> handler, DocumentFiles files, CancellationToken ct,
        DocumentLanguage lang = DocumentLanguage.Ar)
    {
        var upload = await UploadAsync(file, mode, sheet, ct);
        if (upload.IsFailure)
            return upload.Error.ToProblem();
        var preview = await handler.HandleAsync(
            new PreviewCatalogImportQuery(upload.Value.Batch, mode!.Value, upload.Value.Fingerprint, AllIssues: true), ct);
        if (preview.IsFailure)
            return preview.Error.ToProblem();
        var c = await files.ContextAsync(Enum.IsDefined(lang) ? lang : DocumentLanguage.Ar, ct);
        return DocumentFiles.Download(ErrorReport(c, mode.Value, file!.FileName, preview.Value), ExportFormat.Xlsx, c);
    }

    private static TabularReport ErrorReport(
        DocumentContext c, CatalogImportMode mode, string fileName, CatalogImportPreviewResponse preview)
    {
        string SheetName(CatalogImportSheet sheet) => CatalogImportLayout.For(sheet).Name(c.IsArabic);
        string? Column(CatalogImportIssue issue) => issue.Field is null
            ? null
            : CatalogImportLayout.For(issue.Sheet).Columns.FirstOrDefault(x => x.Key == issue.Field)?.Header(c.IsArabic).TrimEnd('*', ' ') ?? issue.Field;
        var totals = preview.Totals;
        return new TabularReport(c.Language, "catalog-import-errors", c.L("أخطاء استيراد بيانات الأصناف", "Catalog import errors"),
            [
                new(c.L("الورقة", "Sheet")),
                new(c.L("الصف", "Row"), ColumnKind.Count),
                new(c.L("العمود", "Column")),
                new(c.L("القيمة", "Value"), ColumnKind.Code),
                new(c.L("الخطأ", "Error")),
                new(c.L("الكود", "Code"), ColumnKind.Code),
            ],
            preview.Issues.Select(i => (IReadOnlyList<object?>)new object?[]
                { SheetName(i.Sheet), i.Row, Column(i), i.Value, CatalogImportMessages.For(c, i), i.Code }).ToList(),
            [
                new(c.L("الملف", "File"), Path.GetFileName(fileName)),
                new(c.L("الوضع", "Mode"), mode switch
                {
                    CatalogImportMode.Create => c.L("إنشاء", "Create"),
                    CatalogImportMode.Update => c.L("تحديث", "Update"),
                    _ => c.L("تخطي", "Skip"),
                }),
            ],
            Summary:
            [
                new(c.L("إجمالي الصفوف", "Rows"), totals.Rows.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new(c.L("صفوف بها أخطاء", "Invalid rows"), totals.Invalid.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new(c.L("صفوف مكررة", "Duplicate rows"), totals.Duplicates.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ]);
    }

    private static async Task<Result<CatalogImportUpload>> UploadAsync(
        IFormFile? file, CatalogImportMode? mode, CatalogImportSheet? sheet, CancellationToken ct)
    {
        if (mode is not { } chosen || !Enum.IsDefined(chosen))
            return Error.Validation([new ValidationError("mode", "Catalog.Import.ModeRequired", "Choose the import mode: Create, Update or Skip.")]);
        return await CatalogImportFile.ReadAsync(file, sheet, ct);
    }
}
