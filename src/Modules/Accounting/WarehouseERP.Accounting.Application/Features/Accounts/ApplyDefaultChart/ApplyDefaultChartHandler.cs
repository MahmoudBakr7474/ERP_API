using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Mappings;
using WarehouseERP.Accounting.Application.Contracts.Accounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Accounts.ApplyDefaultChart;

// The default chart goes in like an import (same rules, one save) and only into an empty chart; afterwards it is edited
// account by account like any chart.
public sealed partial class ApplyDefaultChartHandler(
    IAccountRepository accounts, IAccountMappingRepository mappings, ICurrentUser currentUser, ILogger<ApplyDefaultChartHandler> logger)
    : ICommandHandler<ApplyDefaultChartCommand, ChartImportedResponse>
{
    public async Task<Result<ChartImportedResponse>> HandleAsync(ApplyDefaultChartCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (await accounts.AnyAsync(ct) || await mappings.AnyAsync(ct))
            return AccountErrors.ChartNotEmpty;
        var built = ChartBuilder.Build(DefaultChart.Accounts, accounts.Add);
        if (built.IsFailure)
            throw new InvalidOperationException($"The default chart is invalid: {built.Error}");
        var mapped = ChartBuilder.MapRoles(DefaultChart.Roles, built.Value, mappings.Add);
        if (mapped.IsFailure)
            throw new InvalidOperationException($"The default chart's role mappings are invalid: {mapped.Error}");
        var saved = await accounts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        DefaultChartApplied(logger, built.Value.Count, mapped.Value);
        return new ChartImportedResponse(built.Value.Count, mapped.Value);
    }

    [LoggerMessage(EventId = 9015, Level = LogLevel.Information, Message = "Default chart of accounts applied: {Accounts} accounts, {Roles} roles mapped")]
    private static partial void DefaultChartApplied(ILogger logger, int accounts, int roles);
}
