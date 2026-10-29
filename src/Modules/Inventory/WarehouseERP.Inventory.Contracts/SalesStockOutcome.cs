namespace WarehouseERP.Inventory.Contracts;

/// <summary>
/// What posting a sales delivery or return did: the posted stock document with the cost of each line, or why Inventory
/// refused it. A refusal carries Inventory's own error code, which the caller passes on.
/// </summary>
/// <param name="DocumentId">The posted stock document; null when refused.</param>
/// <param name="Number">Its number (SDL-... or SRT-...); null when refused.</param>
/// <param name="PostedAtUtc">When it posted; null when refused.</param>
/// <param name="Lines">One per request line, in its order; empty when refused.</param>
/// <param name="ErrorCode">Inventory's error code when refused.</param>
/// <param name="ErrorMessage">Inventory's message when refused.</param>
/// <param name="IsConflict">True when the refusal is a conflict with current state (retry later), false for a business rule.</param>
public sealed record SalesStockOutcome(
    Guid? DocumentId, string? Number, DateTime? PostedAtUtc, IReadOnlyList<SalesStockLine> Lines, string? ErrorCode,
    string? ErrorMessage, bool IsConflict)
{
    /// <summary>True when the stock document posted.</summary>
    public bool Succeeded => DocumentId is not null;

    /// <summary>A posted stock document.</summary>
    public static SalesStockOutcome Posted(Guid documentId, string number, DateTime postedAtUtc, IReadOnlyList<SalesStockLine> lines) =>
        new(documentId, number, postedAtUtc, lines, null, null, false);

    /// <summary>A refusal with Inventory's error.</summary>
    public static SalesStockOutcome Refused(string errorCode, string errorMessage, bool isConflict) =>
        new(null, null, null, [], errorCode, errorMessage, isConflict);
}
