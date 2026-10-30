using FluentValidation;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.Journals.CreateOpeningJournal;

public sealed class CreateOpeningJournalValidator : AbstractValidator<CreateOpeningJournalCommand>
{
    public CreateOpeningJournalValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(JournalEntry.MaxDescriptionLength);
        RuleFor(x => x.Reference).MaximumLength(JournalEntry.MaxReferenceLength);
        this.AddLineRules(x => x.Lines, required: true);
    }
}
