using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings;

/// <summary>
/// One line of a document's journal: a posting role's account, or a named ledger account (a cashbox's), with a signed amount
/// (debit positive, credit negative; zero lines are left out) and, on a control account, its party and due date.
/// </summary>
public sealed record SourceLine(PostingRole? Role, Guid? AccountId, decimal Amount, PartyType? PartyType, Guid? PartyId, DateOnly? DueDate);
