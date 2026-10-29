using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Returns.CancelSalesReturn;

public sealed class CancelSalesReturnValidator : AbstractValidator<CancelSalesReturnCommand>
{
    public CancelSalesReturnValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
