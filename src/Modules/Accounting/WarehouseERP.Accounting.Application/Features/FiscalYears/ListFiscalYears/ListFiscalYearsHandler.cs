using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Application.Contracts.FiscalYears;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.FiscalYears.ListFiscalYears;

// A company has a handful of fiscal years, each with twelve periods, so they come whole.
public sealed class ListFiscalYearsHandler(IFiscalYearReadStore years, ICurrentUser currentUser)
    : IQueryHandler<ListFiscalYearsQuery, IReadOnlyList<FiscalYearResponse>>
{
    public async Task<Result<IReadOnlyList<FiscalYearResponse>>> HandleAsync(ListFiscalYearsQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        return Result.Success(await years.ListAsync(ct));
    }
}
