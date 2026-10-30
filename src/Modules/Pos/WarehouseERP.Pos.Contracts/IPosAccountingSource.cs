namespace WarehouseERP.Pos.Contracts;

/// <summary>
/// Read-only view of the tills for Accounting (ADR 0020, Milestone C): each sale and return (one recognition event per
/// transaction), each closed shift's cash, what the drawers held at a moment, and the incidents and differences still to
/// reconcile. Accounting journals them later and never changes anything here, so checkout never waits on Accounting.
/// </summary>
public interface IPosAccountingSource
{
    /// <summary>
    /// Sales, returns and shift closes of the period (at or after <paramref name="fromUtc"/>, before
    /// <paramref name="toUtc"/>), oldest first.
    /// </summary>
    Task<IReadOnlyList<PosPostedDocument>> ListPostedAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct);

    /// <summary>A sale, or null when there is none.</summary>
    Task<PosSaleAccounting?> GetSaleAsync(Guid saleId, CancellationToken ct);

    /// <summary>A return, or null when there is none.</summary>
    Task<PosReturnAccounting?> GetReturnAsync(Guid returnId, CancellationToken ct);

    /// <summary>A closed shift's cash, or null when there is none or it is still open.</summary>
    Task<PosShiftAccounting?> GetClosedShiftAsync(Guid shiftId, CancellationToken ct);

    /// <summary>
    /// Cash the tills had taken just before the moment and not handed in yet: for each shift open then, the cash of its
    /// sales less the cash of its refunds before the moment.
    /// </summary>
    Task<decimal> TillCashBeforeAsync(DateTime beforeUtc, CancellationToken ct);

    /// <summary>Card incidents still waiting for a reversal or a settlement (a card was charged but no sale recorded).</summary>
    Task<IReadOnlyList<PosOpenCardIncident>> ListOpenCardIncidentsAsync(CancellationToken ct);

    /// <summary>Shifts closed at or after the moment whose counted cash differed from what was expected.</summary>
    Task<IReadOnlyList<PosShiftCashDifference>> ListCashDifferencesAsync(DateTime fromUtc, CancellationToken ct);
}
