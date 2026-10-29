namespace WarehouseERP.Inventory.Contracts;

/// <summary>
/// What reserving or releasing did: the base quantity per line, or why Inventory refused. A refusal carries Inventory's
/// own error code (for example <c>Inventory.Stock.Insufficient</c>), which the caller passes on after rolling back.
/// </summary>
/// <param name="Lines">Base units reserved or released per line, in the request's order; empty when refused.</param>
/// <param name="ErrorCode">Inventory's error code when refused.</param>
/// <param name="ErrorMessage">Inventory's message when refused.</param>
/// <param name="IsConflict">True when the refusal is a conflict with current state (retry later), false for a business rule.</param>
public sealed record StockReservationOutcome(
    IReadOnlyList<ReservedQuantity> Lines, string? ErrorCode, string? ErrorMessage, bool IsConflict)
{
    /// <summary>True when every line was reserved or released.</summary>
    public bool Succeeded => ErrorCode is null;

    /// <summary>Every line reserved or released.</summary>
    public static StockReservationOutcome Done(IReadOnlyList<ReservedQuantity> lines) => new(lines, null, null, false);

    /// <summary>A refusal with Inventory's error.</summary>
    public static StockReservationOutcome Refused(string errorCode, string errorMessage, bool isConflict) =>
        new([], errorCode, errorMessage, isConflict);
}
