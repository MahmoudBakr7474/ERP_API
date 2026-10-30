using FluentValidation;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.Journals.ReverseJournal;

public sealed class ReverseJournalValidator : AbstractValidator<ReverseJournalCommand>
{
    public ReverseJournalValidator()
    {
        RuleFor(x => x.Description).MaximumLength(JournalEntry.MaxDescriptionLength);
        RuleFor(x => x.Reference).MaximumLength(JournalEntry.MaxReferenceLength);
    }
}
