using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Returns.ApproveSalesReturn;

public sealed class ApproveSalesReturnValidator : AbstractValidator<ApproveSalesReturnCommand>
{
    public ApproveSalesReturnValidator()
    {
        RuleFor(x => x.RowVersion).MustBeRowVersion();
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header (a new GUID per approval).");
    }
}
