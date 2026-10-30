using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Cutover.ActivateAccounting;

// The explicit, authorized activation (accounting.integration.manage, company-wide) from the settings' version the user
// read: every cutover check must pass at this moment, the cutover's start among them. From then on operational documents
// posted from that start (kept in the settings) are journaled automatically, the start date stays fixed and the opening
// balances can't change. Activation is never undone.
public sealed partial class ActivateAccountingHandler(
    CutoverChecks checks, IAccountingSettingsRepository settings, ICurrentUser currentUser, TimeProvider clock,
    ILogger<ActivateAccountingHandler> logger)
    : ICommandHandler<ActivateAccountingCommand, string>
{
    public async Task<Result<string>> HandleAsync(ActivateAccountingCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var readiness = await checks.CheckAsync(ct);
        if (readiness.IsFailure)
            return readiness.Error;
        if (readiness.Value.IsActivated)
            return WarehouseERP.Accounting.Domain.AccountingSettingsDomainErrors.AlreadyActivated;
        if (!readiness.Value.Ready)
            return CutoverErrors.NotReady(readiness.Value.Checks.Where(c => !c.Passed).Select(c => c.Code));
        var current = await settings.GetForUpdateAsync(expected, ct);
        var activated = current.Activate(currentUser.UserId, clock.GetUtcNow().UtcDateTime, readiness.Value.CutoverStartUtc);
        if (activated.IsFailure)
            return activated.Error;
        var saved = await settings.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        AccountingActivated(logger, current.CutoverDate, currentUser.UserId);
        return Convert.ToBase64String(current.RowVersion);
    }

    [LoggerMessage(EventId = 9220, Level = LogLevel.Information, Message = "Automatic accounting activated from {CutoverDate} by {UserId}")]
    private static partial void AccountingActivated(ILogger logger, DateOnly cutoverDate, Guid? userId);
}
