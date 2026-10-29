using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Orders.SubmitSalesOrder;

public sealed class SubmitSalesOrderValidator : AbstractValidator<SubmitSalesOrderCommand>
{
    public SubmitSalesOrderValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
