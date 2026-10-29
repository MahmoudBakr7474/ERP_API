using FluentValidation;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Application.Features.Quotations.CreateSalesQuotation;

public sealed class CreateSalesQuotationValidator : AbstractValidator<CreateSalesQuotationCommand>
{
    public CreateSalesQuotationValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.CustomerReference).MaximumLength(100);
        RuleFor(x => x.PaymentTermDays!.Value).InclusiveBetween(0, 365).OverridePropertyName("PaymentTermDays")
            .When(x => x.PaymentTermDays.HasValue);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count <= SalesQuotation.MaxLines).WithMessage("A quotation holds at most 200 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(LinePricing.Rules);
    }
}
