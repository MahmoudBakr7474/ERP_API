using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetPartyStatement;

/// <summary>Balances are debit minus credit: a customer's positive balance is owed to us, a supplier's negative balance we owe.</summary>
public sealed record PartyStatementResponse(
    PartyType PartyType, Guid PartyId, string PartyCode, string PartyNameAr, DateOnly FromDate, DateOnly ToDate, string CurrencyCode,
    decimal OpeningBalance, decimal TotalDebit, decimal TotalCredit, decimal ClosingBalance, IReadOnlyList<PartyStatementLineResponse> Lines);
