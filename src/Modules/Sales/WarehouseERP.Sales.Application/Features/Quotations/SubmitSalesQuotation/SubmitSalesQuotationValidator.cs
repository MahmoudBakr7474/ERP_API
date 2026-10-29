using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Quotations.SubmitSalesQuotation;

public sealed class SubmitSalesQuotationValidator : AbstractValidator<SubmitSalesQuotationCommand>
{
    public SubmitSalesQuotationValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
