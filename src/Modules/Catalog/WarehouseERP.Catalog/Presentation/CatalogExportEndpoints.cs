using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Catalog.Application.Contracts.Categories;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.Catalog.Application.Contracts.Units;
using WarehouseERP.Catalog.Application.Features.Categories.GetCategory;
using WarehouseERP.Catalog.Application.Features.Exports.ExportBarcodes;
using WarehouseERP.Catalog.Application.Features.Exports.ExportCategories;
using WarehouseERP.Catalog.Application.Features.Exports.ExportDataQualityIssues;
using WarehouseERP.Catalog.Application.Features.Exports.ExportProducts;
using WarehouseERP.Catalog.Application.Features.Exports.ExportProductUnits;
using WarehouseERP.Catalog.Application.Features.Exports.ExportUnits;
using WarehouseERP.Catalog.Application.Features.Exports.GetProductSheet;
using WarehouseERP.Catalog.Application.Features.Units.GetUnit;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Presentation.Documents;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Presentation;

// Server-generated catalog files (docs/DOCUMENTS.md). Each download runs the Application query of its report with
// the screen's filters over the full matching result (up to the format's row limit, else 422), then the shared
// writers lay it out. Reading the catalog is enough to download it; nothing here changes data.
internal static class CatalogExportEndpoints
{
    public const string Route = "/api/catalog/exports";

    public static IEndpointRouteBuilder MapCatalogExportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("CatalogExports").RequireAuthorization(CatalogPermissions.ProductsRead);

        group.MapGet("/products", ProductsAsync).WithName("ExportProducts").ProducesFile();
        group.MapGet("/product-units", ProductUnitsAsync).WithName("ExportProductUnits").ProducesFile();
        group.MapGet("/barcodes", BarcodesAsync).WithName("ExportBarcodes").ProducesFile();
        group.MapGet("/categories", CategoriesAsync).WithName("ExportCategories").ProducesFile();
        group.MapGet("/units", UnitsAsync).WithName("ExportUnits").ProducesFile();
        group.MapGet("/data-quality", DataQualityAsync).WithName("ExportCatalogDataQualityIssues").ProducesFile();

        app.MapGet(ProductEndpoints.Route + "/{id:guid}/sheet", SheetAsync)
            .WithTags("CatalogExports")
            .RequireAuthorization(CatalogPermissions.ProductsRead)
            .WithName("GetProductSheet")
            .Produces(StatusCodes.Status200OK, contentType: DocumentFiles.PdfContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        return app;
    }

    private static async Task<IResult> ProductsAsync(
        IQueryHandler<ExportProductsQuery, IReadOnlyList<ProductMasterResponse>> handler,
        IQueryHandler<GetCategoryQuery, CategoryDetailsResponse> categories, DocumentFiles files, CancellationToken ct,
        ExportFormat format = ExportFormat.Xlsx, DocumentLanguage lang = DocumentLanguage.Ar, string? search = null,
        Guid? categoryId = null, ProductType? type = null, bool? isActive = null, ProductSort? sort = null, bool descending = false)
    {
        if (Invalid(format, lang) is { } invalid)
            return invalid.ToProblem();
        var rows = await handler.HandleAsync(
            new ExportProductsQuery(search, categoryId, type, isActive, sort, descending, files.RowLimit(format)), ct);
        if (rows.IsFailure)
            return rows.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        var category = await CategoryAsync(categories, categoryId, ct);
        var filters = new ExportFilters(c).Search(search)
            .Named("التصنيف", "Category", category?.Code, category?.NameAr, category?.NameEn)
            .Type(type).Status(isActive).Build();
        return DocumentFiles.Download(CatalogReportDocuments.Products(c, rows.Value, format, filters), format, c);
    }

    private static async Task<IResult> ProductUnitsAsync(
        IQueryHandler<ExportProductUnitsQuery, IReadOnlyList<UnitConversionResponse>> handler,
        IQueryHandler<GetCategoryQuery, CategoryDetailsResponse> categories, IQueryHandler<GetUnitQuery, UnitDetailsResponse> units,
        DocumentFiles files, CancellationToken ct,
        ExportFormat format = ExportFormat.Xlsx, DocumentLanguage lang = DocumentLanguage.Ar, string? search = null,
        Guid? categoryId = null, ProductType? type = null, bool? isActive = null, Guid? unitId = null)
    {
        if (Invalid(format, lang) is { } invalid)
            return invalid.ToProblem();
        var rows = await handler.HandleAsync(
            new ExportProductUnitsQuery(search, categoryId, type, isActive, unitId, files.RowLimit(format)), ct);
        if (rows.IsFailure)
            return rows.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        var category = await CategoryAsync(categories, categoryId, ct);
        var unit = await UnitAsync(units, unitId, ct);
        var filters = new ExportFilters(c).Search(search)
            .Named("التصنيف", "Category", category?.Code, category?.NameAr, category?.NameEn)
            .Named("الوحدة", "Unit", unit?.Code, unit?.NameAr, unit?.NameEn)
            .Type(type).Status(isActive, "حالة الصنف", "Product status").Build();
        return DocumentFiles.Download(CatalogReportDocuments.ProductUnits(c, rows.Value, format, filters), format, c);
    }

    private static async Task<IResult> BarcodesAsync(
        IQueryHandler<ExportBarcodesQuery, IReadOnlyList<BarcodeReportResponse>> handler,
        IQueryHandler<GetCategoryQuery, CategoryDetailsResponse> categories, DocumentFiles files, CancellationToken ct,
        ExportFormat format = ExportFormat.Xlsx, DocumentLanguage lang = DocumentLanguage.Ar, string? search = null,
        Guid? categoryId = null, ProductType? type = null, bool? isActive = null, BarcodeType? barcodeType = null,
        bool? barcodeIsActive = null)
    {
        if (Invalid(format, lang) is { } invalid)
            return invalid.ToProblem();
        var rows = await handler.HandleAsync(
            new ExportBarcodesQuery(search, categoryId, type, isActive, barcodeType, barcodeIsActive, files.RowLimit(format)), ct);
        if (rows.IsFailure)
            return rows.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        var category = await CategoryAsync(categories, categoryId, ct);
        var filters = new ExportFilters(c).Search(search)
            .Named("التصنيف", "Category", category?.Code, category?.NameAr, category?.NameEn)
            .Type(type).Status(isActive, "حالة الصنف", "Product status")
            .Add("نوع الباركود", "Barcode type", barcodeType is { } b ? CatalogLabels.Barcode(c, b) : null)
            .Status(barcodeIsActive, "حالة الباركود", "Barcode status").Build();
        return DocumentFiles.Download(CatalogReportDocuments.Barcodes(c, rows.Value, format, filters), format, c);
    }

    private static async Task<IResult> CategoriesAsync(
        IQueryHandler<ExportCategoriesQuery, IReadOnlyList<CategoryProductCountResponse>> handler, DocumentFiles files,
        CancellationToken ct, ExportFormat format = ExportFormat.Xlsx, DocumentLanguage lang = DocumentLanguage.Ar, bool? isActive = null)
    {
        if (Invalid(format, lang) is { } invalid)
            return invalid.ToProblem();
        var rows = await handler.HandleAsync(new ExportCategoriesQuery(isActive, files.RowLimit(format)), ct);
        if (rows.IsFailure)
            return rows.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        var filters = new ExportFilters(c).Status(isActive, "حالة التصنيف", "Category status").Build();
        return DocumentFiles.Download(CatalogReportDocuments.Categories(c, rows.Value, format, filters), format, c);
    }

    private static async Task<IResult> UnitsAsync(
        IQueryHandler<ExportUnitsQuery, IReadOnlyList<UnitUsageResponse>> handler, DocumentFiles files, CancellationToken ct,
        ExportFormat format = ExportFormat.Xlsx, DocumentLanguage lang = DocumentLanguage.Ar, bool? isActive = null)
    {
        if (Invalid(format, lang) is { } invalid)
            return invalid.ToProblem();
        var rows = await handler.HandleAsync(new ExportUnitsQuery(isActive, files.RowLimit(format)), ct);
        if (rows.IsFailure)
            return rows.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        return DocumentFiles.Download(CatalogReportDocuments.Units(c, rows.Value, new ExportFilters(c).Status(isActive).Build()), format, c);
    }

    private static async Task<IResult> DataQualityAsync(
        IQueryHandler<ExportDataQualityIssuesQuery, IReadOnlyList<DataQualityIssueResponse>> handler, DocumentFiles files,
        CancellationToken ct, ExportFormat format = ExportFormat.Xlsx, DocumentLanguage lang = DocumentLanguage.Ar,
        CatalogDataIssue? issue = null, string? search = null, bool? isActive = null)
    {
        if (Invalid(format, lang) is { } invalid)
            return invalid.ToProblem();
        var rows = await handler.HandleAsync(new ExportDataQualityIssuesQuery(issue, search, isActive, files.RowLimit(format)), ct);
        if (rows.IsFailure)
            return rows.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        var filters = new ExportFilters(c).Search(search)
            .Add("الملاحظة", "Issue", issue is { } i ? CatalogLabels.Issue(c, i) : null)
            .Status(isActive, "حالة الصنف", "Product status").Build();
        return DocumentFiles.Download(CatalogReportDocuments.DataQuality(c, rows.Value, filters), format, c);
    }

    private static async Task<IResult> SheetAsync(
        Guid id, IQueryHandler<GetProductSheetQuery, ProductSheetResponse> handler, DocumentFiles files, CancellationToken ct,
        DocumentLanguage lang = DocumentLanguage.Ar)
    {
        if (Invalid(ExportFormat.Pdf, lang) is { } invalid)
            return invalid.ToProblem();
        var sheet = await handler.HandleAsync(new GetProductSheetQuery(id), ct);
        if (sheet.IsFailure)
            return sheet.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        return DocumentFiles.Download(ProductSheetDocument.Write(c, sheet.Value), ExportFormat.Pdf, "product-sheet", c);
    }

    // A filter's id that names nothing still filters (to no rows); it just has no name to print.
    private static async Task<CategoryDetailsResponse?> CategoryAsync(
        IQueryHandler<GetCategoryQuery, CategoryDetailsResponse> categories, Guid? id, CancellationToken ct) =>
        id is { } categoryId && await categories.HandleAsync(new GetCategoryQuery(categoryId), ct) is { IsSuccess: true } found
            ? found.Value
            : null;

    private static async Task<UnitDetailsResponse?> UnitAsync(
        IQueryHandler<GetUnitQuery, UnitDetailsResponse> units, Guid? id, CancellationToken ct) =>
        id is { } unitId && await units.HandleAsync(new GetUnitQuery(unitId), ct) is { IsSuccess: true } found ? found.Value : null;

    // Enums bind from numbers too ("format=7"): only the named values are files we can write.
    private static Error? Invalid(ExportFormat format, DocumentLanguage lang)
    {
        var errors = new List<ValidationError>();
        if (!Enum.IsDefined(format))
            errors.Add(new ValidationError("format", "Export.Format.Invalid", "Choose Xlsx, Csv or Pdf."));
        if (!Enum.IsDefined(lang))
            errors.Add(new ValidationError("lang", "Export.Language.Invalid", "Choose Ar or En."));
        return errors.Count == 0 ? null : Error.Validation(errors);
    }

    private static RouteHandlerBuilder ProducesFile(this RouteHandlerBuilder builder) =>
        builder.Produces(StatusCodes.Status200OK, responseType: null, DocumentFiles.XlsxContentType,
                DocumentFiles.CsvContentType, DocumentFiles.PdfContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
}
