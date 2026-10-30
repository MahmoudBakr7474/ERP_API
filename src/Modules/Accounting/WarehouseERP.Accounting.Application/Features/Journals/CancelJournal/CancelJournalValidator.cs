using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;

namespace WarehouseERP.Accounting.Application.Features.Journals.CancelJournal;

public sealed class CancelJournalValidator : AbstractValidator<CancelJournalCommand>
{
    public CancelJournalValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
