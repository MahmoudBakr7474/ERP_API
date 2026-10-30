using FluentValidation;
using WarehouseERP.Accounting.Application.Shared;

namespace WarehouseERP.Accounting.Application.Features.Journals.PostJournal;

public sealed class PostJournalValidator : AbstractValidator<PostJournalCommand>
{
    public PostJournalValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header: a new GUID per posting, the same one on a retry.");
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
