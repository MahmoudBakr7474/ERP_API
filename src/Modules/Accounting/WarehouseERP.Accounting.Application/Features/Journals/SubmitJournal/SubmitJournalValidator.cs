using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;

namespace WarehouseERP.Accounting.Application.Features.Journals.SubmitJournal;

public sealed class SubmitJournalValidator : AbstractValidator<SubmitJournalCommand>
{
    public SubmitJournalValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
