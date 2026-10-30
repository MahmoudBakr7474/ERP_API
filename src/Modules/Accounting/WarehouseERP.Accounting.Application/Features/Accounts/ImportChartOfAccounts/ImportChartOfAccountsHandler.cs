using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Mappings;
using WarehouseERP.Accounting.Application.Contracts.Accounts;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Accounts.ImportChartOfAccounts;

// The company's approved chart (the client reads the spreadsheet and sends its rows) replaces the empty chart in one save:
// every row and every role mapping passes, or nothing is created. A chart that already has accounts is never merged.
public sealed partial class ImportChartOfAccountsHandler(
    IAccountRepository accounts, IAccountMappingRepository mappings, ICurrentUser currentUser, ILogger<ImportChartOfAccountsHandler> logger)
    : ICommandHandler<ImportChartOfAccountsCommand, ChartImportedResponse>
{
    public async Task<Result<ChartImportedResponse>> HandleAsync(ImportChartOfAccountsCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (await accounts.AnyAsync(ct) || await mappings.AnyAsync(ct))
            return AccountErrors.ChartNotEmpty;
        var built = ChartBuilder.Build(command.Accounts, accounts.Add);
        if (built.IsFailure)
            return built.Error;
        var mapped = ChartBuilder.MapRoles(command.Roles ?? [], built.Value, mappings.Add);
        if (mapped.IsFailure)
            return mapped.Error;
        var saved = await accounts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ChartImported(logger, built.Value.Count, mapped.Value);
        return new ChartImportedResponse(built.Value.Count, mapped.Value);
    }

    [LoggerMessage(EventId = 9014, Level = LogLevel.Information, Message = "Chart of accounts imported: {Accounts} accounts, {Roles} roles mapped")]
    private static partial void ChartImported(ILogger logger, int accounts, int roles);
}
