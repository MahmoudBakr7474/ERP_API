namespace WarehouseERP.Accounting.Application.Contracts.Journals;

public sealed record JournalLineResponse(
    int LineNumber, Guid AccountId, string AccountCode, string AccountNameAr, string? AccountNameEn, Guid BranchId, string? BranchCode,
    string? BranchNameAr, decimal Debit, decimal Credit, string? Description);
