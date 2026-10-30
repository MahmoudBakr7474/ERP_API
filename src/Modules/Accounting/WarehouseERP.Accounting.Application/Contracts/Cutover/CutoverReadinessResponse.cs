namespace WarehouseERP.Accounting.Application.Contracts.Cutover;

/// <summary>Whether automatic accounting can start at the cutover date, check by check.</summary>
/// <param name="Ready">Every check passes and accounting is not active yet.</param>
/// <param name="CutoverStartUtc">The moment the cutover date starts in the company's time zone (once activated, the moment kept then): documents posted from then on are journaled automatically, everything before belongs to the opening balances.</param>
/// <param name="RowVersion">The settings' version, for activation.</param>
public sealed record CutoverReadinessResponse(
    DateOnly CutoverDate, DateTime CutoverStartUtc, bool IsActivated, DateTime? ActivatedAtUtc, Guid? ActivatedByUserId, bool Ready,
    IReadOnlyList<CutoverCheckResponse> Checks, string RowVersion);
