using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Journals;

// Who edits a journal depends on its type, which is in the journal, so the handlers check it: opening balances need
// accounting.opening.manage, manual journals and reversals accounting.journals.manage. Approving needs
// accounting.journals.approve whatever the type (the endpoint checks it) — and the writer may approve their own journal.
internal static class JournalAccess
{
    public static Result CanManage(ICurrentUser user, JournalType type)
    {
        var access = AccountingAccess.CompanyWide(user);
        if (access.IsFailure)
            return access;
        return AccountingAccess.Holds(user, type == JournalType.OpeningBalance
            ? AccountingPermissions.OpeningBalancesManage
            : AccountingPermissions.JournalsManage);
    }
}
