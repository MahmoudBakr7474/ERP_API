namespace WarehouseERP.SharedKernel.Domain;

/// <summary>
/// A row that records who created and last changed it, and when. The auditing interceptor stamps these fields on
/// save; no caller sets them.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; }
    Guid? CreatedByUserId { get; }
    DateTime? UpdatedAtUtc { get; }
    Guid? UpdatedByUserId { get; }
}
