namespace WarehouseERP.Accounting.Domain;

/// <summary>One line as entered: an account and a branch, an amount on exactly one side, and an optional text.</summary>
public sealed record JournalLineInput(Guid AccountId, Guid BranchId, decimal Debit, decimal Credit, string? Description);
