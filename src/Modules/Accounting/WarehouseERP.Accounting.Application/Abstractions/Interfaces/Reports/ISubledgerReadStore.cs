using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;

/// <summary>Customers' and suppliers' side of the ledger: their posted control-account lines and open items.</summary>
public interface ISubledgerReadStore
{
    /// <summary>
    /// The items of a party type opened on or before the date with what they had open at its end: the amount less the active
    /// allocations dated on or before it. Items with nothing open are left out.
    /// </summary>
    Task<IReadOnlyList<AgingItem>> OpenItemsAsOfAsync(PartyType partyType, DateOnly asOfDate, Guid? branchId, CancellationToken ct);

    /// <summary>A party's posted control-account lines (debit minus credit summed) dated before a date.</summary>
    Task<decimal> PartyBalanceBeforeAsync(PartyType partyType, Guid partyId, DateOnly date, CancellationToken ct);

    /// <summary>A party's posted control-account lines over the dates, in date and number order.</summary>
    Task<IReadOnlyList<LedgerLine>> PartyLinesAsync(PartyType partyType, Guid partyId, DateOnly fromDate, DateOnly toDate, CancellationToken ct);

    /// <summary>Every control account's two sides as of the end of a date.</summary>
    Task<IReadOnlyList<ControlAccountTotals>> ControlTotalsAsync(DateOnly asOfDate, CancellationToken ct);
}
