using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.UpdateSupplierInvoice;

public sealed class UpdateSupplierInvoiceValidator : AbstractValidator<UpdateSupplierInvoiceCommand>
{
    public UpdateSupplierInvoiceValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.SupplierInvoiceNumber).NotEmpty().MaximumLength(SupplierInvoiceNumbers.MaxLength);
        RuleFor(x => x.InvoiceDate).NotEqual(default(DateOnly));
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty()
            .Must(l => l.Count <= SupplierInvoice.MaxLines).WithMessage("An invoice holds at most 200 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(InvoiceLines.Rules);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
