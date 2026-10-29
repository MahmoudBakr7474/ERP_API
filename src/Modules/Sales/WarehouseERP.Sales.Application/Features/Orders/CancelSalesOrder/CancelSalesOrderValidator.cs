using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Orders.CancelSalesOrder;

public sealed class CancelSalesOrderValidator : AbstractValidator<CancelSalesOrderCommand>
{
    public CancelSalesOrderValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
