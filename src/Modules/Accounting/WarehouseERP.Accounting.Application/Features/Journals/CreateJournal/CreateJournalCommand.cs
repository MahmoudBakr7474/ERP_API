using WarehouseERP.Accounting.Application.Contracts.Journals;

namespace WarehouseERP.Accounting.Application.Features.Journals.CreateJournal;

/// <summary>A manual journal draft; it must balance.</summary>
/// <param name="PostingDate">On or after the accounting start date; its period is checked when it posts.</param>
public sealed record CreateJournalCommand(DateOnly PostingDate, string Description, string? Reference, IReadOnlyList<JournalLineRequest> Lines);
