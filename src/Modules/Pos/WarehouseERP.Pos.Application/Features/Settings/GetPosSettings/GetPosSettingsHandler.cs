using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Settings.GetPosSettings;

// Any signed-in user may read the POS policies (a till shows the discount limit).
public sealed class GetPosSettingsHandler(IPosSettingsRepository settings) : IQueryHandler<GetPosSettingsQuery, PosSettingsResponse>
{
    public async Task<Result<PosSettingsResponse>> HandleAsync(GetPosSettingsQuery query, CancellationToken ct)
    {
        var current = await settings.GetAsync(ct);
        return new PosSettingsResponse(current.MaxLineDiscountPercent, current.ReturnWindowDays, current.UpdatedAtUtc,
            Convert.ToBase64String(current.RowVersion));
    }
}
