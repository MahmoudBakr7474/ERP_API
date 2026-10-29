using FluentValidation;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Application.Features.Orders.ReduceSalesOrderLine;

public sealed class ReduceSalesOrderLineValidator : AbstractValidator<ReduceSalesOrderLineCommand>
{
    public ReduceSalesOrderLineValidator()
    {
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(Amounts.MaxQuantity)
            .Must(q => Amounts.FitsScale(q, Amounts.QuantityScale)).WithMessage("'Quantity' may have at most 4 decimals.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(400);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
