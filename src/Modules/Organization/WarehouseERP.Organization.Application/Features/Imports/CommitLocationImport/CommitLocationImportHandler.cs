using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Imports;
using WarehouseERP.Organization.Application.Contracts.Imports;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Imports.CommitLocationImport;

// All or nothing, against the organization as it is now: the batch is planned again (permissions and scope included),
// and only when the outcome is exactly the confirmed preview (same token) and has no problem does it go through the
// domain methods into one save point. A second confirmation of the same file finds its locations existing, so its
// token differs and nothing happens twice. Every created or renamed location keeps its history in the temporal table
// with the importing user as its author.
public sealed partial class CommitLocationImportHandler(
    ILocationImportStore store, ICurrentUser user, ILogger<CommitLocationImportHandler> logger)
    : ICommandHandler<CommitLocationImportCommand, LocationImportResultResponse>
{
    public async Task<Result<LocationImportResultResponse>> HandleAsync(CommitLocationImportCommand command, CancellationToken ct)
    {
        var can = LocationImportPermissions.Of(user);
        if (!can.Any)
            return ImportErrors.NotPermitted;
        var current = await store.LoadAsync(LocationImportPlanner.BranchCodes(command.Batch), ct);
        var plan = LocationImportPlanner.Plan(command.Batch, command.Mode, current, can, WarehouseScope.Of(user));
        if (!string.Equals(plan.Token(command.FileFingerprint), command.PreviewToken, StringComparison.Ordinal))
            return ImportErrors.ChangedSincePreview;
        if (plan.Issues.Count > 0)
            return ImportErrors.HasErrors;
        if (!plan.CanCommit)
            return ImportErrors.NothingToImport;

        var applied = LocationImportWriter.Apply(plan);
        if (applied.IsFailure)
            return applied.Error;
        var saved = await store.SaveAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        var totals = plan.Totals;
        ImportCommitted(logger, user.UserId, command.Mode, totals.Create, totals.Update, totals.Unchanged, totals.Skip, command.FileFingerprint);
        return new LocationImportResultResponse(plan.Mode, totals);
    }

    [LoggerMessage(EventId = 2029, Level = LogLevel.Information,
        Message = "Location import by {UserId} ({Mode}): {Created} created, {Renamed} renamed, {Unchanged} unchanged, {Skipped} skipped; file {Fingerprint}")]
    private static partial void ImportCommitted(
        ILogger logger, Guid? userId, LocationImportMode mode, int created, int renamed, int unchanged, int skipped, string fingerprint);
}
