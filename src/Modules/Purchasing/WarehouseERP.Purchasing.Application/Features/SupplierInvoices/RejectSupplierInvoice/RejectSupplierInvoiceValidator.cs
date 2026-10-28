using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.RejectSupplierInvoice;

public sealed class RejectSupplierInvoiceValidator : AbstractValidator<RejectSupplierInvoiceCommand>
{
    public RejectSupplierInvoiceValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
