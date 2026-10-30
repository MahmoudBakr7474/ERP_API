using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Imports;
using WarehouseERP.Catalog.Application.Contracts.Imports;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Imports.CommitCatalogImport;

// All or nothing, against the catalog as it is now: the batch is planned again, and only when the outcome is exactly
// the confirmed preview (same token) and has no problem does it go through the domain methods into one save point.
// A second confirmation of the same file finds its rows existing, so its token differs and nothing happens twice.
// Every created or changed row keeps its history in the temporal tables with the importing user as its author.
public sealed partial class CommitCatalogImportHandler(
    ICatalogImportStore store, ICurrentUser user, ILogger<CommitCatalogImportHandler> logger)
    : ICommandHandler<CommitCatalogImportCommand, CatalogImportResultResponse>
{
    public async Task<Result<CatalogImportResultResponse>> HandleAsync(CommitCatalogImportCommand command, CancellationToken ct)
    {
        var can = CatalogImportPermissions.Of(user);
        if (!can.Any)
            return ImportErrors.NotPermitted;
        var (skus, barcodes) = CatalogImportPlanner.References(command.Batch);
        var current = await store.LoadAsync(skus, barcodes, ct);
        var plan = CatalogImportPlanner.Plan(command.Batch, command.Mode, current, can);
        if (!string.Equals(plan.Token(command.FileFingerprint), command.PreviewToken, StringComparison.Ordinal))
            return ImportErrors.ChangedSincePreview;
        if (plan.Issues.Count > 0)
            return ImportErrors.HasErrors;
        if (!plan.CanCommit)
            return ImportErrors.NothingToImport;

        var applied = CatalogImportWriter.Apply(plan, current, store);
        if (applied.IsFailure)
            return applied.Error;
        var saved = await store.SaveAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        var totals = plan.Totals;
        ImportCommitted(logger, user.UserId, command.Mode, totals.Create, totals.Update, totals.Unchanged, totals.Skip,
            command.FileFingerprint);
        return new CatalogImportResultResponse(plan.Mode, totals, plan.Sheets);
    }

    [LoggerMessage(EventId = 3050, Level = LogLevel.Information,
        Message = "Catalog import by {UserId} ({Mode}): {Created} created, {Updated} updated, {Unchanged} unchanged, {Skipped} skipped; file {Fingerprint}")]
    private static partial void ImportCommitted(
        ILogger logger, Guid? userId, CatalogImportMode mode, int created, int updated, int unchanged, int skipped, string fingerprint);
}
