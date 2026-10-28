using FluentValidation;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.CreatePurchaseRequest;

public sealed class CreatePurchaseRequestValidator : AbstractValidator<CreatePurchaseRequestCommand>
{
    public CreatePurchaseRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty()
            .Must(l => l.Count <= PurchaseRequest.MaxLines).WithMessage("A request holds at most 200 lines.");
        RuleForEach(x => x.Lines).Cascade(CascadeMode.Continue).ChildRules(RequestLines.Rules);
    }
}
