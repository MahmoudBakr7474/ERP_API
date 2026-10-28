using FluentValidation;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.UpdatePurchaseRequest;

public sealed class UpdatePurchaseRequestValidator : AbstractValidator<UpdatePurchaseRequestCommand>
{
    public UpdatePurchaseRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty()
            .Must(l => l.Count <= PurchaseRequest.MaxLines).WithMessage("A request holds at most 200 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(RequestLines.Rules);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
