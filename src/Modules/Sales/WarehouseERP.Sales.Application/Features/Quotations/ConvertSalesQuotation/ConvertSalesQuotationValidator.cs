using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Quotations.ConvertSalesQuotation;

public sealed class ConvertSalesQuotationValidator : AbstractValidator<ConvertSalesQuotationCommand>
{
    public ConvertSalesQuotationValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
