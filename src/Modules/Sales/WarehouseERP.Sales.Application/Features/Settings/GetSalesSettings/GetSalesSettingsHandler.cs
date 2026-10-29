using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Settings.GetSalesSettings;

public sealed class GetSalesSettingsHandler(ISalesSettingsRepository settings) : IQueryHandler<GetSalesSettingsQuery, SalesSettingsResponse>
{
    public async Task<Result<SalesSettingsResponse>> HandleAsync(GetSalesSettingsQuery query, CancellationToken ct)
    {
        var s = await settings.GetAsync(ct);
        return new SalesSettingsResponse(
            s.QuotationsRequireApproval, s.OrdersRequireApproval, s.DiscountApprovalThreshold, s.ManualPriceRequiresApproval,
            s.PriceBelowListRequiresApproval, s.CreditHoldPolicy, s.CreditHoldBlocksDeliveries, s.DefaultVatRate, s.QuotationValidityDays,
            s.UpdatedAtUtc, Convert.ToBase64String(s.RowVersion));
    }
}
