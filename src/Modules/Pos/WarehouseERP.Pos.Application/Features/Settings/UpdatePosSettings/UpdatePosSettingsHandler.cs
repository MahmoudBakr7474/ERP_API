using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Settings.UpdatePosSettings;

// Company-wide policies need a company-wide user (and pos.settings.manage, checked by the endpoint). Only from the version
// the editor read. Returns the new row version.
public sealed partial class UpdatePosSettingsHandler(
    IPosSettingsRepository settings, ICurrentUser currentUser, ILogger<UpdatePosSettingsHandler> logger)
    : ICommandHandler<UpdatePosSettingsCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdatePosSettingsCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return AccessErrors.CompanyWideRequired;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var current = await settings.GetForUpdateAsync(expected, ct);
        var changed = current.Change(command.MaxLineDiscountPercent, command.ReturnWindowDays);
        if (changed.IsFailure)
            return changed.Error;
        var saved = await settings.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        SettingsChanged(logger, command.MaxLineDiscountPercent, command.ReturnWindowDays);
        return Convert.ToBase64String(current.RowVersion);
    }

    [LoggerMessage(EventId = 8001, Level = LogLevel.Information,
        Message = "POS settings changed; maximum line discount {MaxLineDiscountPercent}%, return window {ReturnWindowDays} days")]
    private static partial void SettingsChanged(ILogger logger, decimal maxLineDiscountPercent, int returnWindowDays);
}
