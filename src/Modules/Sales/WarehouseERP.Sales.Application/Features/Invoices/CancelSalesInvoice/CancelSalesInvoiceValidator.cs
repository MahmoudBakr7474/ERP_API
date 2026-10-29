using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Invoices.CancelSalesInvoice;

public sealed class CancelSalesInvoiceValidator : AbstractValidator<CancelSalesInvoiceCommand>
{
    public CancelSalesInvoiceValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
