using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Journals.ReverseJournal;

// The controlled correction of a posted journal (manual, opening or voucher): a draft with the same lines turned the other
// way, on a date of its own in an open period. The original stays exactly as posted; its open items must be unallocated,
// and the reversal's items settle them when it posts. One active reversal per journal (a filtered unique index decides
// when two users reverse it at once); it copies the manual-journal approval policy.
public sealed partial class ReverseJournalHandler(
    IJournalRepository journals, IAccountingSettingsRepository settings, IOpenItemRepository openItems, ICurrentUser currentUser,
    TimeProvider clock, ILogger<ReverseJournalHandler> logger)
    : ICommandHandler<ReverseJournalCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(ReverseJournalCommand command, CancellationToken ct)
    {
        var access = JournalAccess.CanManage(currentUser, JournalType.Reversal);
        if (access.IsFailure)
            return access.Error;
        var original = await journals.FindAsync(command.JournalId, ct);
        if (original is null)
            return JournalErrors.NotFound;
        if (await journals.HasActiveReversalAsync(original.Id, ct))
            return JournalErrors.AlreadyReversed;
        if (await openItems.HasActiveAllocationsAsync(original.Id, ct))
            return JournalErrors.ItemsAllocated;
        var policies = await settings.GetAsync(ct);
        if (command.PostingDate < policies.CutoverDate)
            return JournalErrors.BeforeCutover(policies.CutoverDate);
        if (command.PostingDate < original.PostingDate)
            return JournalErrors.ReversalBeforeOriginal;
        var created = JournalEntry.CreateReversal(original, command.PostingDate, command.Description, command.Reference,
            policies.ManualJournalsRequireApproval, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        journals.Add(created.Value);
        var saved = await journals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ReversalDrafted(logger, created.Value.Id, original.Id, original.Number);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 9047, Level = LogLevel.Information, Message = "Reversal {JournalId} drafted for journal {OriginalId} ({Number})")]
    private static partial void ReversalDrafted(ILogger logger, Guid journalId, Guid originalId, string? number);
}
