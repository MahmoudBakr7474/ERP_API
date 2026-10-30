using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Organization.Application.Contracts.Branches;
using WarehouseERP.Organization.Application.Contracts.Companies;
using WarehouseERP.Organization.Application.Contracts.Reports;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Application.Features.Branches.GetBranch;
using WarehouseERP.Organization.Application.Features.Companies.GetCompany;
using WarehouseERP.Organization.Application.Features.Exports.ExportBranches;
using WarehouseERP.Organization.Application.Features.Exports.ExportDataIssues;
using WarehouseERP.Organization.Application.Features.Exports.ExportLocations;
using WarehouseERP.Organization.Application.Features.Exports.ExportStructure;
using WarehouseERP.Organization.Application.Features.Exports.ExportWarehouses;
using WarehouseERP.Organization.Application.Features.Warehouses.GetWarehouse;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Presentation.Documents;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Presentation;

// Server-generated organization files (docs/DOCUMENTS.md). Each download runs the Application query of its screen with
// the screen's filters over the full matching result (up to the format's row limit, else 422), then the shared writers
// lay it out. Each file needs the read permission of what it lists; nothing here changes data.
internal static class OrganizationExportEndpoints
{
    public const string Route = "/api/organization/exports";

    public static IEndpointRouteBuilder MapOrganizationExportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("OrganizationExports");

        group.MapGet("/branches", BranchesAsync).WithName("ExportBranches")
            .RequireAuthorization(OrganizationPermissions.BranchesRead).ProducesFile();
        group.MapGet("/warehouses", WarehousesAsync).WithName("ExportWarehouses")
            .RequireAuthorization(OrganizationPermissions.WarehousesRead).ProducesFile();
        group.MapGet("/locations", LocationsAsync).WithName("ExportLocations")
            .RequireAuthorization(OrganizationPermissions.WarehousesRead).ProducesFile().ProducesProblem(StatusCodes.Status403Forbidden);
        group.MapGet("/structure", StructureAsync).WithName("ExportOrganizationStructure")
            .RequireAuthorization(OrganizationPermissions.BranchesRead, OrganizationPermissions.WarehousesRead).ProducesFile();
        group.MapGet("/data-quality", DataQualityAsync).WithName("ExportOrganizationDataIssues")
            .RequireAuthorization(OrganizationPermissions.BranchesRead, OrganizationPermissions.WarehousesRead).ProducesFile();

        // Any signed-in user reads the company (ADR 0013), so its profile too.
        app.MapGet(CompanyEndpoints.Route + "/profile", CompanyProfileAsync)
            .WithTags("OrganizationExports")
            .RequireAuthorization()
            .WithName("GetCompanyProfile")
            .Produces(StatusCodes.Status200OK, contentType: DocumentFiles.PdfContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        return app;
    }

    private static async Task<IResult> BranchesAsync(
        IQueryHandler<ExportBranchesQuery, IReadOnlyList<BranchSummaryResponse>> handler, DocumentFiles files, CancellationToken ct,
        ExportFormat format = ExportFormat.Xlsx, DocumentLanguage lang = DocumentLanguage.Ar, string? search = null,
        bool? isActive = null, bool? hasWarehouses = null, OrganizationSort? sort = null, bool descending = false)
    {
        if (Invalid(format, lang) is { } invalid)
            return invalid.ToProblem();
        var rows = await handler.HandleAsync(new ExportBranchesQuery(search, isActive, hasWarehouses, sort, descending, files.RowLimit(format)), ct);
        if (rows.IsFailure)
            return rows.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        var filters = new ExportFilters(c).Search(search).Status(isActive)
            .YesNo("لها مخازن", "Has warehouses", hasWarehouses).Build();
        return DocumentFiles.Download(OrganizationReportDocuments.Branches(c, rows.Value, format, filters), format, c);
    }

    private static async Task<IResult> WarehousesAsync(
        IQueryHandler<ExportWarehousesQuery, IReadOnlyList<WarehouseSummaryResponse>> handler,
        IQueryHandler<GetBranchQuery, BranchDetailsResponse> branches, DocumentFiles files, CancellationToken ct,
        ExportFormat format = ExportFormat.Xlsx, DocumentLanguage lang = DocumentLanguage.Ar, Guid? branchId = null,
        string? search = null, bool? isActive = null, WarehouseType? type = null, bool? allowsSelling = null,
        OrganizationSort? sort = null, bool descending = false)
    {
        if (Invalid(format, lang) is { } invalid)
            return invalid.ToProblem();
        var rows = await handler.HandleAsync(
            new ExportWarehousesQuery(branchId, search, isActive, type, allowsSelling, sort, descending, files.RowLimit(format)), ct);
        if (rows.IsFailure)
            return rows.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        var branch = await BranchAsync(branches, branchId, ct);
        var filters = new ExportFilters(c).Named("الفرع", "Branch", branch?.Code, branch?.NameAr, branch?.NameEn)
            .Search(search).Type(type).YesNo("يسمح بالبيع", "Allows selling", allowsSelling).Status(isActive).Build();
        return DocumentFiles.Download(OrganizationReportDocuments.Warehouses(c, rows.Value, format, filters), format, c);
    }

    private static async Task<IResult> LocationsAsync(
        IQueryHandler<ExportLocationsQuery, IReadOnlyList<LocationDirectoryResponse>> handler,
        IQueryHandler<GetBranchQuery, BranchDetailsResponse> branches, IQueryHandler<GetWarehouseQuery, WarehouseDetailsResponse> warehouses,
        DocumentFiles files, CancellationToken ct, ExportFormat format = ExportFormat.Xlsx, DocumentLanguage lang = DocumentLanguage.Ar,
        Guid? branchId = null, Guid? warehouseId = null, WarehouseType? warehouseType = null, string? search = null,
        bool? isActive = null, OrganizationSort? sort = null, bool descending = false)
    {
        if (Invalid(format, lang) is { } invalid)
            return invalid.ToProblem();
        var rows = await handler.HandleAsync(
            new ExportLocationsQuery(branchId, warehouseId, warehouseType, search, isActive, sort, descending, files.RowLimit(format)), ct);
        if (rows.IsFailure)
            return rows.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        var branch = await BranchAsync(branches, branchId, ct);
        var warehouse = warehouseId is { } id && await warehouses.HandleAsync(new GetWarehouseQuery(id), ct) is { IsSuccess: true } found
            ? found.Value
            : null;
        var filters = new ExportFilters(c).Named("الفرع", "Branch", branch?.Code, branch?.NameAr, branch?.NameEn)
            .Named("المخزن", "Warehouse", warehouse?.Code, warehouse?.NameAr, warehouse?.NameEn)
            .Type(warehouseType).Search(search).Status(isActive).Build();
        return DocumentFiles.Download(OrganizationReportDocuments.Locations(c, rows.Value, format, filters), format, c);
    }

    private static async Task<IResult> StructureAsync(
        IQueryHandler<ExportStructureQuery, OrganizationStructureExport> handler, DocumentFiles files, CancellationToken ct,
        ExportFormat format = ExportFormat.Xlsx, DocumentLanguage lang = DocumentLanguage.Ar, bool? isActive = null,
        bool includeLocations = false)
    {
        if (Invalid(format, lang) is { } invalid)
            return invalid.ToProblem();
        var export = await handler.HandleAsync(new ExportStructureQuery(isActive, includeLocations, files.RowLimit(format)), ct);
        if (export.IsFailure)
            return export.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        var filters = new ExportFilters(c).Status(isActive).YesNo("يشمل المواقع", "Includes locations", includeLocations).Build();
        return DocumentFiles.Download(OrganizationReportDocuments.Structure(c, export.Value, format, filters), format, c);
    }

    private static async Task<IResult> DataQualityAsync(
        IQueryHandler<ExportDataIssuesQuery, IReadOnlyList<OrganizationDataIssueResponse>> handler, DocumentFiles files,
        CancellationToken ct, ExportFormat format = ExportFormat.Xlsx, DocumentLanguage lang = DocumentLanguage.Ar,
        OrganizationDataIssue? issue = null)
    {
        if (Invalid(format, lang) is { } invalid)
            return invalid.ToProblem();
        var rows = await handler.HandleAsync(new ExportDataIssuesQuery(issue, files.RowLimit(format)), ct);
        if (rows.IsFailure)
            return rows.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        var filters = new ExportFilters(c).Add("الملاحظة", "Issue", issue is { } i ? OrganizationLabels.Issue(c, i) : null).Build();
        return DocumentFiles.Download(OrganizationReportDocuments.DataIssues(c, rows.Value, format, filters), format, c);
    }

    private static async Task<IResult> CompanyProfileAsync(
        IQueryHandler<GetCompanyQuery, CompanyResponse> handler, DocumentFiles files, CancellationToken ct,
        DocumentLanguage lang = DocumentLanguage.Ar)
    {
        if (Invalid(ExportFormat.Pdf, lang) is { } invalid)
            return invalid.ToProblem();
        var company = await handler.HandleAsync(new GetCompanyQuery(), ct);
        if (company.IsFailure)
            return company.Error.ToProblem();
        var c = await files.ContextAsync(lang, ct);
        return DocumentFiles.Download(CompanyProfileDocument.Write(c, company.Value), ExportFormat.Pdf, "company-profile", c);
    }

    // A filter's id that names nothing (or nothing the caller may read) still filters; it just has no name to print.
    private static async Task<BranchDetailsResponse?> BranchAsync(
        IQueryHandler<GetBranchQuery, BranchDetailsResponse> branches, Guid? id, CancellationToken ct) =>
        id is { } branchId && await branches.HandleAsync(new GetBranchQuery(branchId), ct) is { IsSuccess: true } found
            ? found.Value
            : null;

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
