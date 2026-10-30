using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetPartyStatement;

// Posted control-account lines naming the party: the balance before the range, then each line with its running balance.
public sealed class GetPartyStatementHandler(
    ISubledgerReadStore subledger, PartyDirectory parties, AccountingReferences references, ICurrentUser currentUser)
    : IQueryHandler<GetPartyStatementQuery, PartyStatementResponse>
{
    public async Task<Result<PartyStatementResponse>> HandleAsync(GetPartyStatementQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var party = await parties.FindAsync(query.PartyType, query.PartyId, ct);
        if (party is null)
            return VoucherErrors.PartyNotFound;
        var opening = await subledger.PartyBalanceBeforeAsync(query.PartyType, query.PartyId, query.FromDate, ct);
        var lines = await subledger.PartyLinesAsync(query.PartyType, query.PartyId, query.FromDate, query.ToDate, ct);
        var balance = opening;
        var rows = new List<PartyStatementLineResponse>(lines.Count);
        foreach (var line in lines)
        {
            balance += line.Debit - line.Credit;
            rows.Add(new PartyStatementLineResponse(line.PostingDate, line.JournalId, line.JournalNumber, line.JournalType,
                line.Description ?? line.JournalDescription, line.Reference, line.Debit, line.Credit, balance));
        }
        return new PartyStatementResponse(query.PartyType, query.PartyId, party.Code, party.NameAr, query.FromDate, query.ToDate,
            company.Value.BaseCurrencyCode, opening, lines.Sum(l => l.Debit), lines.Sum(l => l.Credit), balance, rows);
    }
}
