using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Settings.GetAccountingSettings;

// Any signed-in user reads the policies (journal screens show the start date and whether approval is needed), with what
// can still change.
public sealed class GetAccountingSettingsHandler(
    IAccountingSettingsRepository settings, IFiscalYearRepository years, IJournalRepository journals, AccountingReferences references)
    : IQueryHandler<GetAccountingSettingsQuery, AccountingSettingsResponse>
{
    public async Task<Result<AccountingSettingsResponse>> HandleAsync(GetAccountingSettingsQuery query, CancellationToken ct)
    {
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var current = await settings.GetAsync(ct);
        return new AccountingSettingsResponse(current.FiscalYearStartMonth, current.CutoverDate, current.ManualJournalsRequireApproval,
            current.VouchersRequireApproval, company.Value.BaseCurrencyCode, await years.AnyAsync(ct),
            current.IsActivated || await journals.AnyPostedAsync(ct), Convert.ToBase64String(current.RowVersion), current.ActivatedAtUtc);
    }
}
