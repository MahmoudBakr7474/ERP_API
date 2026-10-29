using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Features.Settings.GetSalesSettings;

/// <summary>The company's sales policies. Who approves is decided by permission and warehouse scope only.</summary>
/// <param name="DiscountApprovalThreshold">A line discount above this percentage needs approval; null: no threshold.</param>
/// <param name="ManualPriceRequiresApproval">Any manual price (where the list has none, or instead of it) needs approval.</param>
/// <param name="PriceBelowListRequiresApproval">A manual price below the price list needs approval.</param>
/// <param name="CreditHoldBlocksDeliveries">No delivery is posted for a customer on credit hold.</param>
/// <param name="DefaultVatRate">Percent; standard-rated lines take it unless they name another rate.</param>
/// <param name="RowVersion">Send it back with the next change.</param>
public sealed record SalesSettingsResponse(
    bool QuotationsRequireApproval, bool OrdersRequireApproval, decimal? DiscountApprovalThreshold, bool ManualPriceRequiresApproval,
    bool PriceBelowListRequiresApproval, CreditHoldPolicy CreditHoldPolicy, bool CreditHoldBlocksDeliveries, decimal DefaultVatRate,
    int QuotationValidityDays, DateTime? UpdatedAtUtc, string RowVersion);
