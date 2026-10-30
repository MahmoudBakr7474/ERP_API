using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Imports;
using WarehouseERP.Catalog.Application.Contracts.Imports;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Imports.PreviewCatalogImport;

// Plans the import against the current catalog and changes nothing (nothing is saved, the request's DbContext is
// discarded). Anyone who may create or change some catalog data may preview; each row then needs its own permission.
public sealed class PreviewCatalogImportHandler(ICatalogImportStore store, ICurrentUser user)
    : IQueryHandler<PreviewCatalogImportQuery, CatalogImportPreviewResponse>
{
    public async Task<Result<CatalogImportPreviewResponse>> HandleAsync(PreviewCatalogImportQuery query, CancellationToken ct)
    {
        var can = CatalogImportPermissions.Of(user);
        if (!can.Any)
            return ImportErrors.NotPermitted;
        var (skus, barcodes) = CatalogImportPlanner.References(query.Batch);
        var current = await store.LoadAsync(skus, barcodes, ct);
        return CatalogImportPlanner.Plan(query.Batch, query.Mode, current, can)
            .ToPreview(query.FileFingerprint, query.AllIssues ? int.MaxValue : CatalogImportPlan.MaxIssuesShown);
    }
}
