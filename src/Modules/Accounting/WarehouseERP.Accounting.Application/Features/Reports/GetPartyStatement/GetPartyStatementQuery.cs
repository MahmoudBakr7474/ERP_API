using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetPartyStatement;

/// <summary>A customer's or supplier's statement: their control-account lines with a running balance.</summary>
public sealed record GetPartyStatementQuery(PartyType PartyType, Guid PartyId, DateOnly FromDate, DateOnly ToDate);
