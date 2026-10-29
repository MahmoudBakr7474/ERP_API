using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Settings.UpdateSalesSettings;

// Company-wide policies need a company-wide user (and sales.settings.manage, checked by the endpoint). Only from the
// version the editor read. Returns the new row version.
public sealed partial class UpdateSalesSettingsHandler(
    ISalesSettingsRepository settings, ICurrentUser currentUser, ILogger<UpdateSalesSettingsHandler> logger)
    : ICommandHandler<UpdateSalesSettingsCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateSalesSettingsCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return AccessErrors.CompanyWideRequired;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var current = await settings.GetForUpdateAsync(expected, ct);
        var changed = current.Change(
            command.QuotationsRequireApproval, command.OrdersRequireApproval, command.DiscountApprovalThreshold,
            command.ManualPriceRequiresApproval, command.PriceBelowListRequiresApproval, command.CreditHoldPolicy,
            command.CreditHoldBlocksDeliveries, command.DefaultVatRate, command.QuotationValidityDays);
        if (changed.IsFailure)
            return changed.Error;
        var saved = await settings.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        SettingsChanged(logger, command.OrdersRequireApproval, command.DefaultVatRate);
        return Convert.ToBase64String(current.RowVersion);
    }

    [LoggerMessage(EventId = 7010, Level = LogLevel.Information,
        Message = "Sales settings changed; orders require approval: {OrdersRequireApproval}, default VAT {DefaultVatRate}")]
    private static partial void SettingsChanged(ILogger logger, bool ordersRequireApproval, decimal defaultVatRate);
}
