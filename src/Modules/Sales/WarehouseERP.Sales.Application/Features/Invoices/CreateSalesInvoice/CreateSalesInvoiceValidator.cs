using FluentValidation;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Application.Features.Invoices.CreateSalesInvoice;

public sealed class CreateSalesInvoiceValidator : AbstractValidator<CreateSalesInvoiceCommand>
{
    public CreateSalesInvoiceValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerReference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count <= SalesInvoice.MaxLines).WithMessage("An invoice holds at most 200 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(InvoiceLines.Rules);
    }
}
