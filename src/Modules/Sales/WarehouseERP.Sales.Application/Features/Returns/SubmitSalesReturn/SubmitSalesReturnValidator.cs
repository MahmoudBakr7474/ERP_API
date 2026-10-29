using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Returns.SubmitSalesReturn;

public sealed class SubmitSalesReturnValidator : AbstractValidator<SubmitSalesReturnCommand>
{
    public SubmitSalesReturnValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
