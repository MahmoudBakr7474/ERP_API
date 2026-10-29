using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Deliveries.PostSalesDelivery;

public sealed class PostSalesDeliveryValidator : AbstractValidator<PostSalesDeliveryCommand>
{
    public PostSalesDeliveryValidator()
    {
        RuleFor(x => x.RowVersion).MustBeRowVersion();
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header (a new GUID per posting).");
    }
}
