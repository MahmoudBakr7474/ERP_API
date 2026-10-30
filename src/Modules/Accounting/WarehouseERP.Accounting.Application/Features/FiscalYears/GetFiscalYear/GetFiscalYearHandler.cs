using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Application.Contracts.FiscalYears;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.FiscalYears.GetFiscalYear;

public sealed class GetFiscalYearHandler(IFiscalYearReadStore years, ICurrentUser currentUser)
    : IQueryHandler<GetFiscalYearQuery, FiscalYearResponse>
{
    public async Task<Result<FiscalYearResponse>> HandleAsync(GetFiscalYearQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        return await years.GetAsync(query.FiscalYearId, ct) is { } year ? year : FiscalYearErrors.NotFound;
    }
}
