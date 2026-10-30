using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.Accounting.Application.Features.Settings.GetAccountingSettings;
using WarehouseERP.Accounting.Application.Features.Settings.UpdateAccountingSettings;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;

namespace WarehouseERP.Accounting.Presentation;

// Any signed-in user reads the accounting policies (journal screens show the start date and the approval policy); changing
// them needs accounting.settings.manage and company-wide access (checked by the handler).
internal static class AccountingSettingsEndpoints
{
    public const string Route = "/api/accounting/settings";

    public static IEndpointRouteBuilder MapAccountingSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("AccountingSettings");

        group.MapGet("/", (IQueryHandler<GetAccountingSettingsQuery, AccountingSettingsResponse> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new GetAccountingSettingsQuery(), ct)))
            .RequireAuthorization()
            .WithName("GetAccountingSettings")
            .Produces<AccountingSettingsResponse>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/", (UpdateAccountingSettingsCommand command, ICommandHandler<UpdateAccountingSettingsCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(command, ct)))
            .RequireAuthorization(AccountingPermissions.SettingsManage)
            .WithName("UpdateAccountingSettings")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        return app;
    }
}
