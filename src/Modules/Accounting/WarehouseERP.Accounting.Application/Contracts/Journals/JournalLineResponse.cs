using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.Journals;

/// <param name="PartyType">The customer or supplier a control-account line names.</param>
public sealed record JournalLineResponse(
    int LineNumber, Guid AccountId, string AccountCode, string AccountNameAr, string? AccountNameEn, Guid BranchId, string? BranchCode,
    string? BranchNameAr, decimal Debit, decimal Credit, string? Description, PartyType? PartyType, Guid? PartyId, string? PartyCode,
    string? PartyNameAr, DateOnly? DueDate);
