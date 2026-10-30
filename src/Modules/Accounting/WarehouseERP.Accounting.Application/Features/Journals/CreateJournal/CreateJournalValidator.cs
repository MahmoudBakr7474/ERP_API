using FluentValidation;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.Journals.CreateJournal;

public sealed class CreateJournalValidator : AbstractValidator<CreateJournalCommand>
{
    public CreateJournalValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(JournalEntry.MaxDescriptionLength);
        RuleFor(x => x.Reference).MaximumLength(JournalEntry.MaxReferenceLength);
        this.AddLineRules(x => x.Lines, required: true);
    }
}
