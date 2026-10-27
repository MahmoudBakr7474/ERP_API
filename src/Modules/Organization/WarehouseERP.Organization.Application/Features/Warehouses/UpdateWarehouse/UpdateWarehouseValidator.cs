using FluentValidation;
using WarehouseERP.Organization.Application.Shared;

namespace WarehouseERP.Organization.Application.Features.Warehouses.UpdateWarehouse;

// Input shape only. The Transit/selling rule is a domain invariant (Warehouse.Update).
public sealed class UpdateWarehouseValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop; // one error per field: the first rule that fails
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x => x.NameEn).MaximumLength(150);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
