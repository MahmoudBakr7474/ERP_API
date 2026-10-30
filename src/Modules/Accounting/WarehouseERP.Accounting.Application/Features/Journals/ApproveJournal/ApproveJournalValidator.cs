using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;

namespace WarehouseERP.Accounting.Application.Features.Journals.ApproveJournal;

public sealed class ApproveJournalValidator : AbstractValidator<ApproveJournalCommand>
{
    public ApproveJournalValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header: a new GUID per approval, the same one on a retry.");
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
