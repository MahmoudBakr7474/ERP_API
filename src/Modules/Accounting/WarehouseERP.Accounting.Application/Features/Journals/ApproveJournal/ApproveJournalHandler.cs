using WarehouseERP.Accounting.Application.Contracts.Journals;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Journals.ApproveJournal;

// An approver (accounting.journals.approve, checked by the endpoint; company-wide) — whoever wrote or submitted the journal
// included — accepts it, and approving posts it. No creator/approver comparison exists anywhere (CLAUDE.md).
public sealed class ApproveJournalHandler(JournalPosting posting, ICurrentUser currentUser)
    : ICommandHandler<ApproveJournalCommand, JournalPostedResponse>
{
    public async Task<Result<JournalPostedResponse>> HandleAsync(ApproveJournalCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        return await posting.PostAsync(command.JournalId, expected, command.IdempotencyKey, byApproval: true, ct);
    }
}
