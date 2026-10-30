using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Contracts.Journals;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Journals.PostJournal;

// Direct posting of a draft that needs no approval, by whoever manages its type of journal.
public sealed class PostJournalHandler(IJournalRepository journals, JournalPosting posting, ICurrentUser currentUser)
    : ICommandHandler<PostJournalCommand, JournalPostedResponse>
{
    public async Task<Result<JournalPostedResponse>> HandleAsync(PostJournalCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var journal = await journals.FindAsync(command.JournalId, ct);
        var access = JournalAccess.CanManage(currentUser, journal?.JournalType ?? JournalType.Manual);
        if (access.IsFailure)
            return access.Error;
        if (journal is null)
            return JournalErrors.NotFound;
        return await posting.PostAsync(command.JournalId, expected, command.IdempotencyKey, byApproval: false, ct);
    }
}
