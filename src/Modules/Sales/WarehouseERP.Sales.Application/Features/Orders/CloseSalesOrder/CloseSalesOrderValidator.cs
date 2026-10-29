using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Orders.CloseSalesOrder;

public sealed class CloseSalesOrderValidator : AbstractValidator<CloseSalesOrderCommand>
{
    public CloseSalesOrderValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
