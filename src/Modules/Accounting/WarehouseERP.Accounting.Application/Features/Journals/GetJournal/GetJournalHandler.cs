using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Contracts.Journals;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Journals.GetJournal;

public sealed class GetJournalHandler(IJournalReadStore journals, ICurrentUser currentUser) : IQueryHandler<GetJournalQuery, JournalResponse>
{
    public async Task<Result<JournalResponse>> HandleAsync(GetJournalQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        return await journals.GetAsync(query.JournalId, ct) is { } journal ? journal : JournalErrors.NotFound;
    }
}
