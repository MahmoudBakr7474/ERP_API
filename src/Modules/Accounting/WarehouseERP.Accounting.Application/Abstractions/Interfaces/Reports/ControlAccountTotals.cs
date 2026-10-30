namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;

/// <summary>
/// A control account as of a date, from both sides: its ledger balance, the part of it on lines without a party (should be
/// none), and its open items (their amounts, and what is still open of them).
/// </summary>
public sealed record ControlAccountTotals(Guid AccountId, decimal LedgerBalance, decimal LinesWithoutParty, decimal ItemsAmount, decimal ItemsOpenAmount);
