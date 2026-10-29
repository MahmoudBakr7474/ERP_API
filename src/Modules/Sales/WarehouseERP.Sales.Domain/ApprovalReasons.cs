using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Domain;

/// <summary>A document's approval reasons as stored: one bit per <see cref="ApprovalReason"/>.</summary>
public static class ApprovalReasons
{
    public static int ToFlags(IEnumerable<ApprovalReason> reasons) => reasons.Aggregate(0, (flags, r) => flags | (1 << (int)r));

    public static IReadOnlyList<ApprovalReason> FromFlags(int flags) =>
        Enum.GetValues<ApprovalReason>().Where(r => (flags & (1 << (int)r)) != 0).ToList();

    /// <summary>The reasons as a note for the document's history.</summary>
    public static string Note(IEnumerable<ApprovalReason> reasons) => string.Join(", ", reasons);
}
