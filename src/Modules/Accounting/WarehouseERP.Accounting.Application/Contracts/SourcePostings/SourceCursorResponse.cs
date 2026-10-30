namespace WarehouseERP.Accounting.Application.Contracts.SourcePostings;

/// <summary>How far documents of one module have been looked for.</summary>
public sealed record SourceCursorResponse(string Module, DateTime ScannedToUtc, DateTime UpdatedAtUtc);
