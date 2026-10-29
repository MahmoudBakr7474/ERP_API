using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Invoices.PostSalesInvoice;

public sealed class PostSalesInvoiceValidator : AbstractValidator<PostSalesInvoiceCommand>
{
    public PostSalesInvoiceValidator()
    {
        RuleFor(x => x.RowVersion).MustBeRowVersion();
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Send an Idempotency-Key header (a new GUID per posting).");
    }
}
