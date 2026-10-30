using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.FiscalYears.CreateFiscalYear;

// A year of twelve open monthly periods from the settings' start month. The first year may start anywhere; each later one
// starts the day after the last one ends, so no date is ever covered twice or left uncovered between years.
public sealed partial class CreateFiscalYearHandler(
    IFiscalYearRepository years, IAccountingSettingsRepository settings, ICurrentUser currentUser, TimeProvider clock,
    ILogger<CreateFiscalYearHandler> logger)
    : ICommandHandler<CreateFiscalYearCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateFiscalYearCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var policies = await settings.GetAsync(ct);
        var created = FiscalYear.Create(new DateOnly(command.StartYear, policies.FiscalYearStartMonth, 1), currentUser.UserId,
            clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        var year = created.Value;
        if (await years.OverlapsAsync(year.StartDate, year.EndDate, ct))
            return FiscalYearErrors.Exists;
        if (await years.LatestAsync(ct) is { } latest && year.StartDate != latest.EndDate.AddDays(1))
            return FiscalYearErrors.NotContiguous;
        years.Add(year);
        var saved = await years.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        YearCreated(logger, year.Id, year.Name);
        return year.Id;
    }

    [LoggerMessage(EventId = 9030, Level = LogLevel.Information, Message = "Fiscal year {FiscalYearId} ({Name}) created")]
    private static partial void YearCreated(ILogger logger, Guid fiscalYearId, string name);
}
