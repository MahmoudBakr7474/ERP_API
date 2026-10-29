using FluentValidation;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Application.Features.Quotations.UpdateSalesQuotation;

public sealed class UpdateSalesQuotationValidator : AbstractValidator<UpdateSalesQuotationCommand>
{
    public UpdateSalesQuotationValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ValidUntil).GreaterThanOrEqualTo(x => x.QuotationDate);
        RuleFor(x => x.CustomerReference).MaximumLength(100);
        RuleFor(x => x.PaymentTermDays).InclusiveBetween(0, 365);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count <= SalesQuotation.MaxLines).WithMessage("A quotation holds at most 200 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(LinePricing.Rules);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
