using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.Journals.RejectJournal;

public sealed class RejectJournalValidator : AbstractValidator<RejectJournalCommand>
{
    public RejectJournalValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(JournalEntry.MaxNoteLength);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
