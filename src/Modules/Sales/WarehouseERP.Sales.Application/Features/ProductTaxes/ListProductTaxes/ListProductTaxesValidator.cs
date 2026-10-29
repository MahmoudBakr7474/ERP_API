using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Sales.Application.Features.ProductTaxes.ListProductTaxes;

public sealed class ListProductTaxesValidator : AbstractValidator<ListProductTaxesQuery>
{
    public ListProductTaxesValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.ProductId).NotEqual(Guid.Empty).When(x => x.ProductId.HasValue);
        RuleFor(x => x.TaxTreatment!.Value).IsInEnum().OverridePropertyName("TaxTreatment").When(x => x.TaxTreatment.HasValue);
    }
}
