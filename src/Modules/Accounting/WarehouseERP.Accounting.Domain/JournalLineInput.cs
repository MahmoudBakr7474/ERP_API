using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// One line as entered: an account and a branch, an amount on exactly one side, and an optional text. A line on a customer
/// or supplier control account also names its party and may carry the date it falls due (its posting date otherwise).
/// </summary>
public sealed record JournalLineInput(
    Guid AccountId, Guid BranchId, decimal Debit, decimal Credit, string? Description, PartyType? PartyType = null, Guid? PartyId = null,
    DateOnly? DueDate = null);
