using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.Journals.UpdateJournal;

public sealed class UpdateJournalValidator : AbstractValidator<UpdateJournalCommand>
{
    public UpdateJournalValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(JournalEntry.MaxDescriptionLength);
        RuleFor(x => x.Reference).MaximumLength(JournalEntry.MaxReferenceLength);
        this.AddLineRules(x => x.Lines, required: false);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
