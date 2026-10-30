namespace WarehouseERP.Accounting.Presentation.Contracts.Journals;

/// <param name="PostingDate">On or after the reversed journal's date.</param>
public sealed record ReverseJournalRequest(DateOnly PostingDate, string? Description, string? Reference);
