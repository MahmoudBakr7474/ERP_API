using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.Accounting.Application.Features.AccountMappings.ListAccountMappings;
using WarehouseERP.Accounting.Application.Features.AccountMappings.RemoveAccountMapping;
using WarehouseERP.Accounting.Application.Features.AccountMappings.SetAccountMapping;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Presentation.Contracts.AccountMappings;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Presentation;

// Which account each posting role uses: read with accounting.accounts.read, changed with accounting.accounts.manage.
internal static class AccountMappingEndpoints
{
    public const string Route = "/api/accounting/account-mappings";

    public static IEndpointRouteBuilder MapAccountMappingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("AccountMappings");

        group.MapGet("/", (IQueryHandler<ListAccountMappingsQuery, IReadOnlyList<AccountMappingResponse>> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new ListAccountMappingsQuery(), ct)))
            .RequireAuthorization(AccountingPermissions.AccountsRead)
            .WithName("ListAccountMappings")
            .Produces<IReadOnlyList<AccountMappingResponse>>()
            .QueryProblems();

        group.MapPut("/{role}", (PostingRole role, SetAccountMappingRequest request, ICommandHandler<SetAccountMappingCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new SetAccountMappingCommand(role, request.AccountId, request.RowVersion), ct)))
            .RequireAuthorization(AccountingPermissions.AccountsManage)
            .WithName("SetAccountMapping")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapDelete("/{role}", (PostingRole role, ICommandHandler<RemoveAccountMappingCommand, Done> handler, CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new RemoveAccountMappingCommand(role), ct)))
            .RequireAuthorization(AccountingPermissions.AccountsManage)
            .WithName("RemoveAccountMapping")
            .Produces(StatusCodes.Status204NoContent)
            .CommandProblems();

        return app;
    }
}
