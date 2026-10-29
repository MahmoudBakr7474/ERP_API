using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Orders.ApproveSalesOrder;

public sealed class ApproveSalesOrderValidator : AbstractValidator<ApproveSalesOrderCommand>
{
    public ApproveSalesOrderValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
