using FluentValidation;
using WarehouseERP.Sales.Application.Shared;

namespace WarehouseERP.Sales.Application.Features.Quotations.ApproveSalesQuotation;

public sealed class ApproveSalesQuotationValidator : AbstractValidator<ApproveSalesQuotationCommand>
{
    public ApproveSalesQuotationValidator() => RuleFor(x => x.RowVersion).MustBeRowVersion();
}
