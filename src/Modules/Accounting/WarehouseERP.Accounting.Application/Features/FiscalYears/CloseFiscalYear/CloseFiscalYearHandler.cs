using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Mappings;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.FiscalYears.CloseFiscalYear;

// Year-end close, in one transaction under the year's lock: every period closed, every earlier year closed, then one
// closing journal dated the year's last day zeroes each revenue and expense account per branch against the account mapped
// to RetainedEarnings (per branch too, so branch figures stay whole). It posts by itself into the closed last period,
// numbered in the CJ series, and the year closes with it. Nothing with a balance → no journal. The closing treatment is
// subject to the accountant's sign-off (V1 decisions).
public sealed partial class CloseFiscalYearHandler(
    ITransactionRunner transactions, IFiscalYearRepository years, IJournalRepository journals, IAccountMappingRepository mappings,
    IAccountRepository accounts, ILedgerReadStore ledger, LedgerPosting posting, IAccountingLocks locks, AccountingReferences references,
    ICurrentUser currentUser, TimeProvider clock, ILogger<CloseFiscalYearHandler> logger)
    : ICommandHandler<CloseFiscalYearCommand, FiscalYearClosedResponse>
{
    public async Task<Result<FiscalYearClosedResponse>> HandleAsync(CloseFiscalYearCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        return await transactions.RunAsync(token => CloseOnceAsync(command.FiscalYearId, expected, token), ct);
    }

    private async Task<Result<FiscalYearClosedResponse>> CloseOnceAsync(Guid yearId, byte[] expected, CancellationToken ct)
    {
        if (!await locks.TryLockYearAsync(yearId, ct))
            return FiscalYearErrors.Busy;
        var year = await years.GetForUpdateAsync(yearId, expected, ct);
        if (year is null)
            return FiscalYearErrors.NotFound;
        if (!year.RowVersion.AsSpan().SequenceEqual(expected))
            return CommonErrors.ConcurrencyConflict;
        if (year.IsClosed)
            return FiscalYearDomainErrors.YearClosed;
        if (year.Periods.Any(p => p.IsOpen))
            return FiscalYearDomainErrors.PeriodsStillOpen;
        if (await years.AnyOpenBeforeAsync(year.StartDate, ct))
            return FiscalYearErrors.EarlierYearOpen;

        var userId = currentUser.UserId;
        var now = clock.GetUtcNow().UtcDateTime;
        var totals = await ledger.IncomeStatementTotalsByBranchAsync(year.StartDate, year.EndDate, ct);
        var netProfit = totals.Sum(t => t.Credit - t.Debit);
        JournalEntry? closing = null;
        var lines = ClosingLines(totals);
        if (lines.Count > 0)
        {
            var retained = await RetainedEarningsAsync(ct);
            if (retained.IsFailure)
                return retained.Error;
            lines.AddRange(totals.GroupBy(t => t.BranchId)
                .Select(g => (BranchId: g.Key, Profit: g.Sum(t => t.Credit - t.Debit)))
                .Where(b => b.Profit != 0m)
                .OrderBy(b => b.BranchId)
                .Select(b => new JournalLineInput(retained.Value, b.BranchId, b.Profit < 0 ? -b.Profit : 0m, b.Profit > 0 ? b.Profit : 0m,
                    null)));
            var company = await references.CompanyAsync(ct);
            if (company.IsFailure)
                return company.Error;
            var created = JournalEntry.CreateGenerated(JournalType.YearEndClosing, year.EndDate, $"إقفال السنة المالية {year.Name}",
                company.Value.BaseCurrencyCode, lines, reversalOfJournalId: null, userId, now);
            if (created.IsFailure)
                return created.Error;
            var slot = await posting.ReserveAsync(JournalType.YearEndClosing, year.EndDate, intoClosedPeriod: true, ct);
            if (slot.IsFailure)
                return slot.Error;
            closing = created.Value;
            closing.MarkPostedGenerated(slot.Value.Number, slot.Value.FiscalPeriodId, userId, now);
            journals.Add(closing);
        }

        var closed = year.Close(closing?.Id, closing?.Number, userId, now);
        if (closed.IsFailure)
            return closed.Error;
        var saved = await years.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        YearClosed(logger, year.Name, closing?.Number, netProfit);
        return new FiscalYearClosedResponse(year.Id, closing?.Id, closing?.Number, netProfit, Convert.ToBase64String(year.RowVersion));
    }

    // Each revenue and expense balance turned the other way, per account and branch, in code-independent stable order.
    private static List<JournalLineInput> ClosingLines(IReadOnlyList<AccountBranchTotals> totals) =>
        totals.Where(t => t.Debit != t.Credit)
            .OrderBy(t => t.AccountId).ThenBy(t => t.BranchId)
            .Select(t => t.Debit > t.Credit
                ? new JournalLineInput(t.AccountId, t.BranchId, 0m, t.Debit - t.Credit, null)
                : new JournalLineInput(t.AccountId, t.BranchId, t.Credit - t.Debit, 0m, null))
            .ToList();

    private async Task<Result<Guid>> RetainedEarningsAsync(CancellationToken ct)
    {
        var mapping = await mappings.FindAsync(PostingRole.RetainedEarnings, ct);
        if (mapping is null)
            return MappingErrors.RoleNotMapped(PostingRole.RetainedEarnings);
        var account = await accounts.FindAsync(mapping.AccountId, ct)
            ?? throw new InvalidOperationException($"Retained earnings are mapped to account {mapping.AccountId}, which does not exist.");
        var fits = PostingRoleRules.Check(PostingRole.RetainedEarnings, account);
        return fits.IsFailure ? fits.Error : account.Id;
    }

    [LoggerMessage(EventId = 9033, Level = LogLevel.Information,
        Message = "Fiscal year {Name} closed with closing journal {Number}; net profit {NetProfit}")]
    private static partial void YearClosed(ILogger logger, string name, string? number, decimal netProfit);
}
