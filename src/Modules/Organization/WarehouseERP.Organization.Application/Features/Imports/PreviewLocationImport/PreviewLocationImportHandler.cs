using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Imports;
using WarehouseERP.Organization.Application.Contracts.Imports;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Imports.PreviewLocationImport;

// Plans the import against the current organization and changes nothing (nothing is saved; the request's DbContext is
// discarded). Anyone who may add or rename locations may preview; each row then needs its own permission and scope.
public sealed class PreviewLocationImportHandler(ILocationImportStore store, ICurrentUser user)
    : IQueryHandler<PreviewLocationImportQuery, LocationImportPreviewResponse>
{
    public async Task<Result<LocationImportPreviewResponse>> HandleAsync(PreviewLocationImportQuery query, CancellationToken ct)
    {
        var can = LocationImportPermissions.Of(user);
        if (!can.Any)
            return ImportErrors.NotPermitted;
        var current = await store.LoadAsync(LocationImportPlanner.BranchCodes(query.Batch), ct);
        return LocationImportPlanner.Plan(query.Batch, query.Mode, current, can, WarehouseScope.Of(user))
            .ToPreview(query.FileFingerprint, query.AllIssues ? int.MaxValue : LocationImportPlan.MaxIssuesShown);
    }
}
