using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Quotations.DeclineSalesQuotation;

public sealed class DeclineSalesQuotationValidator : AbstractValidator<DeclineSalesQuotationCommand>
{
    public DeclineSalesQuotationValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
