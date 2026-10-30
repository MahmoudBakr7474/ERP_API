using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.ReadModels;

// What Sales may learn about a customer's receivable once accounting is active (before it, receivables are not tracked and
// Sales keeps its partial view): the posted balance of their control-account lines, the cutover's start, and which of
// their Sales invoices and returns are journaled already.
internal sealed class CustomerReceivables(DbContext db) : ICustomerReceivables
{
    public async Task<CustomerReceivable?> GetAsync(Guid customerId, CancellationToken ct)
    {
        var start = await db.Set<AccountingSettings>().AsNoTracking().Select(x => x.CutoverStartUtc).SingleOrDefaultAsync(ct);
        if (start is not { } cutoverStart)
            return null;
        var lines = from line in db.Set<JournalLine>().AsNoTracking()
                    join journal in db.Set<JournalEntry>().AsNoTracking() on line.JournalEntryId equals journal.Id
                    where journal.Status == JournalStatus.Posted && line.PartyType == PartyType.Customer && line.PartyId == customerId
                    select new { line.Debit, line.Credit, journal.SourceType, journal.SourceId };
        var balance = await lines.SumAsync(x => x.Debit - x.Credit, ct);
        var journaled = await lines
            .Where(x => x.SourceType == SourceDocumentType.SalesInvoice || x.SourceType == SourceDocumentType.SalesReturn)
            .Select(x => x.SourceId!.Value)
            .Distinct()
            .ToListAsync(ct);
        return new CustomerReceivable(balance, cutoverStart, journaled);
    }
}
