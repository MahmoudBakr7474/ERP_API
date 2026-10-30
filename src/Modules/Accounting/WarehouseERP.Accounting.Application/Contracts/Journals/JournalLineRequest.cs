namespace WarehouseERP.Accounting.Application.Contracts.Journals;

/// <summary>One line as the user enters it: an amount on exactly one side.</summary>
public sealed record JournalLineRequest(Guid AccountId, Guid BranchId, decimal Debit, decimal Credit, string? Description);
