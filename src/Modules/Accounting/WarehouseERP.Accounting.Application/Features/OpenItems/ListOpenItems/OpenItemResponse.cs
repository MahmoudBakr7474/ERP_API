using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.OpenItems.ListOpenItems;

/// <summary>A customer's or supplier's open item; amounts are debit minus credit (receivables positive, payables negative).</summary>
/// <param name="DaysOverdue">Days past its due date as of today in the company's calendar (zero when not yet due).</param>
public sealed record OpenItemResponse(
    Guid Id, PartyType PartyType, Guid PartyId, string? PartyCode, string? PartyNameAr, Guid AccountId, string AccountCode, Guid BranchId,
    Guid JournalId, string DocumentNumber, JournalType JournalType, string? Reference, DateOnly DocumentDate, DateOnly DueDate,
    decimal Amount, decimal OpenAmount, int DaysOverdue, IReadOnlyList<OpenItemAllocationResponse> Allocations);
