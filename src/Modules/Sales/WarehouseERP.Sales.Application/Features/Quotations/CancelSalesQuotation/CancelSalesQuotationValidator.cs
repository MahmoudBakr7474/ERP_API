using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Quotations.CancelSalesQuotation;

public sealed class CancelSalesQuotationValidator : AbstractValidator<CancelSalesQuotationCommand>
{
    public CancelSalesQuotationValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
