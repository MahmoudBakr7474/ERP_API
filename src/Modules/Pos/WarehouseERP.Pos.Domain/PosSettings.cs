using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Domain;

/// <summary>
/// The company's POS policies: one row, edited by company-wide users. The largest line discount a cashier with
/// pos.discounts.apply may give (seeded 10%), and how many days after a sale its goods may come back (seeded 14). A sale
/// or return applies them when it is made.
/// </summary>
public sealed class PosSettings : IAuditable
{
    public const int SingletonId = 1;
    public const int MaxReturnWindowDays = 365;

    private PosSettings() { } // EF Core

    public int Id { get; private set; }

    /// <summary>The largest line discount, in percent, anyone may give at a till.</summary>
    public decimal MaxLineDiscountPercent { get; private set; }

    /// <summary>Days after the sale (company calendar) its goods may still be returned; 0 means the same day only.</summary>
    public int ReturnWindowDays { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public Result Change(decimal maxLineDiscountPercent, int returnWindowDays)
    {
        if (maxLineDiscountPercent is < 0 or > 100 || !Money.FitsScale(maxLineDiscountPercent, Money.PercentScale))
            return PosSettingsDomainErrors.MaxDiscountInvalid;
        if (returnWindowDays is < 0 or > MaxReturnWindowDays)
            return PosSettingsDomainErrors.ReturnWindowInvalid;
        MaxLineDiscountPercent = maxLineDiscountPercent;
        ReturnWindowDays = returnWindowDays;
        return Result.Success();
    }
}
