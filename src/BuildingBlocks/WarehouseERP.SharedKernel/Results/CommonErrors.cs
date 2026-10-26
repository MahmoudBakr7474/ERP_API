namespace WarehouseERP.SharedKernel.Results;

/// <summary>Errors that are not owned by one module.</summary>
public static class CommonErrors
{
    /// <summary>Optimistic concurrency: the row changed since the caller read it.</summary>
    public static readonly Error ConcurrencyConflict = Error.Conflict(
        "Common.ConcurrencyConflict", "The record was changed by someone else. Reload it and try again.");
}
