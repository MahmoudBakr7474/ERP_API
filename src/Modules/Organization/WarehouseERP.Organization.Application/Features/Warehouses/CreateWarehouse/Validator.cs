using FluentValidation;

namespace WarehouseERP.Organization.Application.Features.Warehouses.CreateWarehouse;

// Input shape only. The Transit/selling rule is a domain invariant (Warehouse.Create) and the
// unique code per branch is guarded again by the database index.
public sealed class CreateWarehouseValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.BranchId).NotEmpty();
        // varchar(30) column: ASCII only, so nothing is lost on save.
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.NameEn).MaximumLength(150);
        RuleFor(x => x.Type).IsInEnum();
    }
}
