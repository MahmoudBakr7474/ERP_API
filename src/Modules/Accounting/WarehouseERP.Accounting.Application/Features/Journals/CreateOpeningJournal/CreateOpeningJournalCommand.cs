using WarehouseERP.Accounting.Application.Contracts.Journals;

namespace WarehouseERP.Accounting.Application.Features.Journals.CreateOpeningJournal;

/// <summary>
/// An opening-balance draft, dated on the accounting start date and always approved. It balances by its own lines: a
/// difference goes on the opening-balance equity account only when the accountant writes that line.
/// </summary>
public sealed record CreateOpeningJournalCommand(string Description, string? Reference, IReadOnlyList<JournalLineRequest> Lines);
