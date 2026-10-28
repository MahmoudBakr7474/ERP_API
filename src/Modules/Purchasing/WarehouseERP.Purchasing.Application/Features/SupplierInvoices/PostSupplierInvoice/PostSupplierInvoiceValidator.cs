using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.PostSupplierInvoice;

public sealed class PostSupplierInvoiceValidator : AbstractValidator<PostSupplierInvoiceCommand>
{
    public PostSupplierInvoiceValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header: a new GUID per posting, the same one on a retry.");
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
