using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.RejectSupplierReturn;

public sealed class RejectSupplierReturnValidator : AbstractValidator<RejectSupplierReturnCommand>
{
    public RejectSupplierReturnValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
