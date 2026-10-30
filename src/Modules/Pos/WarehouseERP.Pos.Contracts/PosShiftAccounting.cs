namespace WarehouseERP.Pos.Contracts;

/// <summary>
/// What a closed shift hands in. CashTakings: The cash of its sales less the cash of its refunds: what its transactions
/// put in the drawer. CashDifference: Counted less expected cash: negative when short, positive when over.
/// </summary>
public sealed record PosShiftAccounting(
    Guid Id, string Number, Guid BranchId, Guid TerminalId, DateTime ClosedAtUtc, decimal CashTakings, decimal CountedCash,
    decimal ExpectedCash, decimal CashDifference, bool IsForceClosed);
