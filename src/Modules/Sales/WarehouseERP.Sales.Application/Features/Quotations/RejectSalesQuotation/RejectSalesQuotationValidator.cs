using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Quotations.RejectSalesQuotation;

public sealed class RejectSalesQuotationValidator : AbstractValidator<RejectSalesQuotationCommand>
{
    public RejectSalesQuotationValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
