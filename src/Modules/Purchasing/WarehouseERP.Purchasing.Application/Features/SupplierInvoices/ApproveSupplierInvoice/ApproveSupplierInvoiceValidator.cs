using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.ApproveSupplierInvoice;

public sealed class ApproveSupplierInvoiceValidator : AbstractValidator<ApproveSupplierInvoiceCommand>
{
    public ApproveSupplierInvoiceValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header: a new GUID per approval, the same one on a retry.");
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
