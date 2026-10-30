using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.Journals;

/// <summary>
/// One line as the user enters it: an amount on exactly one side. A line on a customer or supplier control account names
/// its party (and may say when it falls due; its posting date otherwise).
/// </summary>
public sealed record JournalLineRequest(
    Guid AccountId, Guid BranchId, decimal Debit, decimal Credit, string? Description, PartyType? PartyType = null, Guid? PartyId = null,
    DateOnly? DueDate = null);
