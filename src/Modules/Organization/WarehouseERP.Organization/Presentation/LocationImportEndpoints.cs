using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Organization.Application.Contracts.Imports;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Application.Features.Exports.ExportWarehouses;
using WarehouseERP.Organization.Application.Features.Imports.CommitLocationImport;
using WarehouseERP.Organization.Application.Features.Imports.PreviewLocationImport;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Presentation.Imports;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Presentation;

// Storage location import in two steps (docs/modules/Organization/REPORTS_AND_IMPORTS.md): preview shows what each row
// would do and every problem, changing nothing; commit sends the same file, mode and the preview's token, and imports
// everything or nothing. Reading warehouses opens these endpoints; adding and renaming then need the location
// permissions and the warehouse in scope, row by row. Uploads are capped below Kestrel's default; the Identity
// middleware has already checked the anti-forgery header of a cookie session, so the form binding's own check is off.
internal static class LocationImportEndpoints
{
    public const string Route = "/api/organization/imports/locations";

    // The file limit plus room for the multipart envelope and the small form fields.
    private static readonly long MaxRequestBytes = LocationImportFile.Limits.MaxFileBytes + 64 * 1024;

    public static IEndpointRouteBuilder MapLocationImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("OrganizationImports").RequireAuthorization(OrganizationPermissions.WarehousesRead);

        group.MapGet("/template", TemplateAsync)
            .WithName("GetLocationImportTemplate")
            .Produces(StatusCodes.Status200OK, contentType: DocumentFiles.XlsxContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/preview", PreviewAsync)
            .WithName("PreviewLocationImport")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .Produces<LocationImportPreviewResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/commit", CommitAsync)
            .WithName("CommitLocationImport")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .Produces<LocationImportResultResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/error-report", ErrorReportAsync)
            .WithName("GetLocationImportErrorReport")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .Produces(StatusCodes.Status200OK, contentType: DocumentFiles.XlsxContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    // The Lists sheet: the physical warehouses the caller may import into (their scope), by branch.
    private static async Task<IResult> TemplateAsync(
        IQueryHandler<ExportWarehousesQuery, IReadOnlyList<WarehouseSummaryResponse>> warehouses, DocumentFiles files,
        CancellationToken ct, DocumentLanguage lang = DocumentLanguage.Ar)
    {
        if (!Enum.IsDefined(lang))
            return Error.Validation([new ValidationError("lang", "Export.Language.Invalid", "Choose Ar or En.")]).ToProblem();
        var physical = await warehouses.HandleAsync(
            new ExportWarehousesQuery(null, null, null, WarehouseType.Physical, null, null, false, files.RowLimit(ExportFormat.Xlsx)), ct);
        if (physical.IsFailure)
            return physical.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        var ordered = physical.Value.OrderBy(w => w.BranchCode, StringComparer.OrdinalIgnoreCase).ThenBy(w => w.Code, StringComparer.OrdinalIgnoreCase).ToList();
        return DocumentFiles.Download(LocationImportTemplate.Write(c, ordered), ExportFormat.Xlsx, "location-import-template", c);
    }

    private static async Task<IResult> PreviewAsync(
        IFormFile? file, [FromForm] LocationImportMode? mode,
        IQueryHandler<PreviewLocationImportQuery, LocationImportPreviewResponse> handler, CancellationToken ct)
    {
        var upload = await UploadAsync(file, mode, ct);
        if (upload.IsFailure)
            return upload.Error.ToProblem();
        return (await handler.HandleAsync(new PreviewLocationImportQuery(upload.Value.Batch, mode!.Value, upload.Value.Fingerprint), ct))
            .ToHttpResult(TypedResults.Ok);
    }

    private static async Task<IResult> CommitAsync(
        IFormFile? file, [FromForm] LocationImportMode? mode, [FromForm] string? previewToken,
        ICommandHandler<CommitLocationImportCommand, LocationImportResultResponse> handler, CancellationToken ct)
    {
        var upload = await UploadAsync(file, mode, ct);
        if (upload.IsFailure)
            return upload.Error.ToProblem();
        return (await handler.HandleAsync(
                new CommitLocationImportCommand(upload.Value.Batch, mode!.Value, upload.Value.Fingerprint, previewToken ?? ""), ct))
            .ToHttpResult(TypedResults.Ok);
    }

    // Every problem of the preview, in a workbook the user can sort and fix from (the screen shows the first 1,000).
    private static async Task<IResult> ErrorReportAsync(
        IFormFile? file, [FromForm] LocationImportMode? mode,
        IQueryHandler<PreviewLocationImportQuery, LocationImportPreviewResponse> handler, DocumentFiles files, CancellationToken ct,
        DocumentLanguage lang = DocumentLanguage.Ar)
    {
        var upload = await UploadAsync(file, mode, ct);
        if (upload.IsFailure)
            return upload.Error.ToProblem();
        var preview = await handler.HandleAsync(
            new PreviewLocationImportQuery(upload.Value.Batch, mode!.Value, upload.Value.Fingerprint, AllIssues: true), ct);
        if (preview.IsFailure)
            return preview.Error.ToProblem();
        var c = await files.ContextAsync(Enum.IsDefined(lang) ? lang : DocumentLanguage.Ar, ct);
        return DocumentFiles.Download(ErrorReport(c, mode.Value, file!.FileName, preview.Value), ExportFormat.Xlsx, c);
    }

    private static TabularReport ErrorReport(DocumentContext c, LocationImportMode mode, string fileName, LocationImportPreviewResponse preview)
    {
        var totals = preview.Totals;
        string Count(int value) => value.ToString(CultureInfo.InvariantCulture);
        return new TabularReport(c.Language, "location-import-errors", c.L("أخطاء استيراد مواقع التخزين", "Storage location import errors"),
            [
                new(c.L("الصف", "Row"), ColumnKind.Count),
                new(c.L("العمود", "Column")),
                new(c.L("القيمة", "Value"), ColumnKind.Code),
                new(c.L("الخطأ", "Error")),
                new(c.L("الكود", "Code"), ColumnKind.Code),
            ],
            preview.Issues.Select(i => (IReadOnlyList<object?>)new object?[]
                { i.Row, i.Field is null ? null : LocationImportLayout.Header(i.Field, c.IsArabic), i.Value, LocationImportMessages.For(c, i), i.Code }).ToList(),
            [
                new(c.L("الملف", "File"), Path.GetFileName(fileName)),
                new(c.L("الوضع", "Mode"), mode switch
                {
                    LocationImportMode.Create => c.L("إنشاء", "Create"),
                    LocationImportMode.Update => c.L("تحديث", "Update"),
                    _ => c.L("تخطي", "Skip"),
                }),
            ],
            Summary:
            [
                new(c.L("إجمالي الصفوف", "Rows"), Count(totals.Rows)),
                new(c.L("صفوف بها أخطاء", "Invalid rows"), Count(totals.Invalid)),
                new(c.L("صفوف مكررة", "Duplicate rows"), Count(totals.Duplicates)),
            ]);
    }

    private static async Task<Result<LocationImportUpload>> UploadAsync(IFormFile? file, LocationImportMode? mode, CancellationToken ct)
    {
        if (mode is not { } chosen || !Enum.IsDefined(chosen))
            return Error.Validation([new ValidationError("mode", "Organization.Import.ModeRequired", "Choose the import mode: Create, Update or Skip.")]);
        return await LocationImportFile.ReadAsync(file, ct);
    }
}
