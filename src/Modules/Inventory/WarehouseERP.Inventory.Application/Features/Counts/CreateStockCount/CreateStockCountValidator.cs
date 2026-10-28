using FluentValidation;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Application.Features.Counts.CreateStockCount;

public sealed class CreateStockCountValidator : AbstractValidator<CreateStockCountCommand>
{
    public CreateStockCountValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.LocationIds!.Count).LessThanOrEqualTo(StockCount.MaxLocations)
            .OverridePropertyName(nameof(CreateStockCountCommand.LocationIds)).When(x => x.LocationIds is not null);
        RuleForEach(x => x.LocationIds).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
