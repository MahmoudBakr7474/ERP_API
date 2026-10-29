using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Returns.RejectSalesReturn;

public sealed class RejectSalesReturnValidator : AbstractValidator<RejectSalesReturnCommand>
{
    public RejectSalesReturnValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
