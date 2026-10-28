using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Counts.EnterStockCountLines;

// Counting goes on while the scope stays frozen; each save is short and holds no lock. Counted quantities are in base
// units, for countable stock items, in the count's locations. Returns the new row version.
public sealed partial class EnterStockCountLinesHandler(
    IStockCountRepository counts, StockReferences references, ICurrentUser currentUser, ILogger<EnterStockCountLinesHandler> logger)
    : ICommandHandler<EnterStockCountLinesCommand, string>
{
    public async Task<Result<string>> HandleAsync(EnterStockCountLinesCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await CountAccess.AuthorizeAsync(counts, currentUser, command.CountId, ct);
        if (access.IsFailure)
            return access.Error;
        var count = await counts.GetForUpdateAsync(command.CountId, expected, ct);
        if (count is null)
            return StockCountErrors.NotFound;

        var products = await references.CountableProductsAsync(command.Lines.Select(l => l.ProductId).ToList(), ct);
        if (products.IsFailure)
            return products.Error;
        var locations = await references.WarehouseLocationsAsync(count.WarehouseId, command.Lines.Select(l => l.LocationId).ToList(), ct);
        if (locations.IsFailure)
            return locations.Error;
        var baseUnits = await references.BaseUnitsAsync(command.Lines.Select(l => l.ProductId).Distinct().ToList(), ct);
        var entries = command.Lines
            .Select(l => new CountEntry(l.ProductId, l.LocationId, l.CountedQuantity, l.UnitCost, baseUnits[l.ProductId].UnitAllowsFraction))
            .ToList();

        var entered = count.EnterCounts(entries);
        if (entered.IsFailure)
            return entered.Error;
        var saved = await counts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        LinesEntered(logger, count.Id, entries.Count);
        return Convert.ToBase64String(count.RowVersion);
    }

    [LoggerMessage(EventId = 5032, Level = LogLevel.Information, Message = "Stock count {CountId}: {Lines} counted lines entered")]
    private static partial void LinesEntered(ILogger logger, Guid countId, int lines);
}
