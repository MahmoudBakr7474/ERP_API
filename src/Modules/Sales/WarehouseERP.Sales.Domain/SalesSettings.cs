using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// The company's sales policies: one row, edited by company-wide users. Whether quotations and orders always need
/// approval (seeded: no), the optional thresholds that make a single document need it (a discount above a percentage,
/// manual prices, prices below the list), what a customer's credit hold does, and the default VAT rate of standard-rated
/// lines. A document applies them when it is submitted; changing them later never alters a document already issued or
/// confirmed. Who approves is decided by permission and scope only (CLAUDE.md, Document approvals).
/// </summary>
public sealed class SalesSettings : IAuditable
{
    public const int SingletonId = 1;

    private SalesSettings() { } // EF Core

    public int Id { get; private set; }
    public bool QuotationsRequireApproval { get; private set; }
    public bool OrdersRequireApproval { get; private set; }

    /// <summary>A line discount above this percentage needs approval; null: no threshold.</summary>
    public decimal? DiscountApprovalThreshold { get; private set; }

    /// <summary>A manual price (set where the list has none, or instead of it) needs approval.</summary>
    public bool ManualPriceRequiresApproval { get; private set; }

    /// <summary>A manual price below the list's price needs approval.</summary>
    public bool PriceBelowListRequiresApproval { get; private set; }

    public CreditHoldPolicy CreditHoldPolicy { get; private set; }

    /// <summary>No delivery is posted for a customer on credit hold.</summary>
    public bool CreditHoldBlocksDeliveries { get; private set; }

    /// <summary>The VAT rate, in percent, that standard-rated lines take unless they name another.</summary>
    public decimal DefaultVatRate { get; private set; }

    /// <summary>Days a new quotation stays valid unless it says otherwise.</summary>
    public int QuotationValidityDays { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public Result Change(
        bool quotationsRequireApproval, bool ordersRequireApproval, decimal? discountApprovalThreshold, bool manualPriceRequiresApproval,
        bool priceBelowListRequiresApproval, CreditHoldPolicy creditHoldPolicy, bool creditHoldBlocksDeliveries, decimal defaultVatRate,
        int quotationValidityDays)
    {
        if (discountApprovalThreshold is { } threshold && (threshold is < 0 or >= 100 || !Amounts.FitsScale(threshold, Amounts.PercentScale)))
            return SalesSettingsDomainErrors.DiscountThresholdInvalid;
        if (!Enum.IsDefined(creditHoldPolicy))
            return SalesSettingsDomainErrors.CreditHoldPolicyInvalid;
        if (defaultVatRate is <= 0 or > 100 || !Amounts.FitsScale(defaultVatRate, Amounts.PercentScale))
            return SalesSettingsDomainErrors.DefaultVatRateInvalid;
        if (quotationValidityDays is < 1 or > 365)
            return SalesSettingsDomainErrors.QuotationValidityInvalid;

        QuotationsRequireApproval = quotationsRequireApproval;
        OrdersRequireApproval = ordersRequireApproval;
        DiscountApprovalThreshold = discountApprovalThreshold;
        ManualPriceRequiresApproval = manualPriceRequiresApproval;
        PriceBelowListRequiresApproval = priceBelowListRequiresApproval;
        CreditHoldPolicy = creditHoldPolicy;
        CreditHoldBlocksDeliveries = creditHoldBlocksDeliveries;
        DefaultVatRate = defaultVatRate;
        QuotationValidityDays = quotationValidityDays;
        return Result.Success();
    }

    /// <summary>
    /// Why a quotation (<paramref name="isOrder"/> false) or an order with these lines needs approval now; empty when it
    /// needs none. A credit hold counts only for an order, and only when the policy asks for approval rather than refusal.
    /// </summary>
    public IReadOnlyList<ApprovalReason> ApprovalReasons(IReadOnlyCollection<PricedLine> lines, bool isOrder, bool customerOnCreditHold)
    {
        var reasons = new List<ApprovalReason>();
        if (isOrder ? OrdersRequireApproval : QuotationsRequireApproval)
            reasons.Add(ApprovalReason.Policy);
        if (DiscountApprovalThreshold is { } threshold && lines.Any(l => l.DiscountPercent > threshold))
            reasons.Add(ApprovalReason.DiscountAboveThreshold);
        if (PriceBelowListRequiresApproval
            && lines.Any(l => l.PriceSource == PriceSource.Manual && l.ListUnitPrice is { } list && l.UnitPrice < list))
            reasons.Add(ApprovalReason.PriceBelowList);
        if (ManualPriceRequiresApproval && lines.Any(l => l.PriceSource == PriceSource.Manual))
            reasons.Add(ApprovalReason.ManualPrice);
        if (isOrder && customerOnCreditHold && CreditHoldPolicy == CreditHoldPolicy.RequireApproval)
            reasons.Add(ApprovalReason.CreditHold);
        return reasons;
    }
}
