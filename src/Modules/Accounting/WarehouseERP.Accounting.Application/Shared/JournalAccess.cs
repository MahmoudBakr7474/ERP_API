using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Shared;

// Who edits a journal depends on its type, which is in the journal, so the handlers check it: opening balances need
// accounting.opening.manage; customer receipts and refunds accounting.receivables.manage; supplier payments, refunds and
// expense bills accounting.payables.manage; cash vouchers and transfers accounting.treasury.manage; manual journals and
// reversals accounting.journals.manage. Approving needs accounting.journals.approve whatever the type (the endpoint checks
// it) — and the writer may approve their own journal.
internal static class JournalAccess
{
    public static Result CanManage(ICurrentUser user, JournalType type)
    {
        var access = AccountingAccess.CompanyWide(user);
        if (access.IsFailure)
            return access;
        return AccountingAccess.Holds(user, PermissionFor(type));
    }

    public static string PermissionFor(JournalType type) => type switch
    {
        JournalType.OpeningBalance => AccountingPermissions.OpeningBalancesManage,
        JournalType.CustomerReceipt or JournalType.CustomerRefund => AccountingPermissions.ReceivablesManage,
        JournalType.SupplierPayment or JournalType.SupplierRefund or JournalType.ExpenseBill => AccountingPermissions.PayablesManage,
        JournalType.CashReceipt or JournalType.CashPayment or JournalType.CashTransfer => AccountingPermissions.TreasuryManage,
        _ => AccountingPermissions.JournalsManage,
    };
}
