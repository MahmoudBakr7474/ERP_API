using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Orders.RejectSalesOrder;

public sealed class RejectSalesOrderValidator : AbstractValidator<RejectSalesOrderCommand>
{
    public RejectSalesOrderValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
