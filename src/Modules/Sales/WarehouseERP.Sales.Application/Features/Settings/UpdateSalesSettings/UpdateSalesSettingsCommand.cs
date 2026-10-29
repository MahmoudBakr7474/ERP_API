using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Features.Settings.UpdateSalesSettings;

/// <summary>The company's sales policies; documents already issued or confirmed keep what applied to them.</summary>
/// <param name="DiscountApprovalThreshold">0 to below 100; null for no threshold.</param>
/// <param name="DefaultVatRate">Above 0 and up to 100 percent.</param>
/// <param name="QuotationValidityDays">1 to 365.</param>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record UpdateSalesSettingsCommand(
    bool QuotationsRequireApproval, bool OrdersRequireApproval, decimal? DiscountApprovalThreshold, bool ManualPriceRequiresApproval,
    bool PriceBelowListRequiresApproval, CreditHoldPolicy CreditHoldPolicy, bool CreditHoldBlocksDeliveries, decimal DefaultVatRate,
    int QuotationValidityDays, string RowVersion);
