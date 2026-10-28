using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.ApproveSupplierReturn;

public sealed class ApproveSupplierReturnValidator : AbstractValidator<ApproveSupplierReturnCommand>
{
    public ApproveSupplierReturnValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header: a new GUID per approval, the same one on a retry.");
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
