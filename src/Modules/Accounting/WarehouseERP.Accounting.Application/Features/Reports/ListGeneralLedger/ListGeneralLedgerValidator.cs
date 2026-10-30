using FluentValidation;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Features.Reports.ListGeneralLedger;

public sealed class ListGeneralLedgerValidator : AbstractValidator<ListGeneralLedgerQuery>
{
    public ListGeneralLedgerValidator(IOptions<PaginationOptions> pagination)
    {
        this.AddPagingRules(pagination.Value);
        RuleFor(x => x.FromDate).GreaterThanOrEqualTo(new DateOnly(2000, 1, 1));
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
        RuleFor(x => x.AccountId).NotEqual(Guid.Empty).When(x => x.AccountId.HasValue);
        RuleFor(x => x.BranchId).NotEqual(Guid.Empty).When(x => x.BranchId.HasValue);
        RuleFor(x => x.JournalType).IsInEnum();
    }
}
