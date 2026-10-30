namespace WarehouseERP.Accounting.Domain;

/// <summary>An open item a voucher draft means to settle; it becomes an <see cref="Allocation"/> when the voucher posts.</summary>
public sealed class VoucherAllocation
{
    private VoucherAllocation() { } // EF Core

    public long Id { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public Guid OpenItemId { get; private set; }
    public decimal Amount { get; private set; }

    internal static VoucherAllocation Create(Guid openItemId, decimal amount) => new() { OpenItemId = openItemId, Amount = amount };
}
