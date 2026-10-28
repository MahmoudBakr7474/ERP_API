namespace WarehouseERP.Inventory.Contracts;

/// <summary>
/// What posting a purchase receipt or return did: the posted stock document, or why Inventory refused it. A refusal
/// carries Inventory's own error code (for example <c>Inventory.Stock.Insufficient</c>), which the caller passes on.
/// </summary>
/// <param name="DocumentId">The posted stock document; null when refused.</param>
/// <param name="Number">Its number (PRC-... or PRT-...); null when refused.</param>
/// <param name="PostedAtUtc">When it posted; null when refused.</param>
/// <param name="ErrorCode">Inventory's error code when refused.</param>
/// <param name="ErrorMessage">Inventory's message when refused.</param>
/// <param name="IsConflict">True when the refusal is a conflict with current state (retry later), false for a business rule.</param>
public sealed record PurchaseStockOutcome(
    Guid? DocumentId, string? Number, DateTime? PostedAtUtc, string? ErrorCode, string? ErrorMessage, bool IsConflict)
{
    /// <summary>True when the stock document posted.</summary>
    public bool Succeeded => DocumentId is not null;

    /// <summary>A posted stock document.</summary>
    public static PurchaseStockOutcome Posted(Guid documentId, string number, DateTime postedAtUtc) =>
        new(documentId, number, postedAtUtc, null, null, false);

    /// <summary>A refusal with Inventory's error.</summary>
    public static PurchaseStockOutcome Refused(string errorCode, string errorMessage, bool isConflict) =>
        new(null, null, null, errorCode, errorMessage, isConflict);
}
