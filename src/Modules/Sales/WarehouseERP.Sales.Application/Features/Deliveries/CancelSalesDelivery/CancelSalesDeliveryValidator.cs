using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Deliveries.CancelSalesDelivery;

public sealed class CancelSalesDeliveryValidator : AbstractValidator<CancelSalesDeliveryCommand>
{
    public CancelSalesDeliveryValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
