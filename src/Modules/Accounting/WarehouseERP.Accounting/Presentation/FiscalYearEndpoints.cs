using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.Accounting.Application.Contracts.FiscalYears;
using WarehouseERP.Accounting.Application.Features.FiscalYears.CloseFiscalPeriod;
using WarehouseERP.Accounting.Application.Features.FiscalYears.CloseFiscalYear;
using WarehouseERP.Accounting.Application.Features.FiscalYears.CreateFiscalYear;
using WarehouseERP.Accounting.Application.Features.FiscalYears.GetFiscalYear;
using WarehouseERP.Accounting.Application.Features.FiscalYears.ListFiscalYears;
using WarehouseERP.Accounting.Application.Features.FiscalYears.ReopenFiscalPeriod;
using WarehouseERP.Accounting.Application.Features.FiscalYears.ReopenFiscalYear;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Presentation.Contracts.Common;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Presentation;

// Fiscal years and their periods: read with accounting.accounts.read; creating years, closing periods and closing a year
// need accounting.periods.manage; reopening a period or a year accounting.periods.reopen and a reason. All company-wide.
internal static class FiscalYearEndpoints
{
    public const string Route = "/api/accounting/fiscal-years";
    public const string PeriodsRoute = "/api/accounting/fiscal-periods";

    public static IEndpointRouteBuilder MapFiscalYearEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("FiscalYears");

        group.MapGet("/", (IQueryHandler<ListFiscalYearsQuery, IReadOnlyList<FiscalYearResponse>> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new ListFiscalYearsQuery(), ct)))
            .RequireAuthorization(AccountingPermissions.AccountsRead)
            .WithName("ListFiscalYears")
            .Produces<IReadOnlyList<FiscalYearResponse>>()
            .QueryProblems();

        group.MapGet("/{id:guid}", (Guid id, IQueryHandler<GetFiscalYearQuery, FiscalYearResponse> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new GetFiscalYearQuery(id), ct)))
            .RequireAuthorization(AccountingPermissions.AccountsRead)
            .WithName("GetFiscalYear")
            .Produces<FiscalYearResponse>()
            .QueryProblems();

        group.MapPost("/", (CreateFiscalYearCommand command, ICommandHandler<CreateFiscalYearCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(AccountingPermissions.PeriodsManage)
            .WithName("CreateFiscalYear")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPost("/{id:guid}/close", (Guid id, AccountingActionRequest request,
                    ICommandHandler<CloseFiscalYearCommand, FiscalYearClosedResponse> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new CloseFiscalYearCommand(id, request.RowVersion), ct)))
            .RequireAuthorization(AccountingPermissions.PeriodsManage)
            .WithName("CloseFiscalYear")
            .Produces<FiscalYearClosedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/reopen", (Guid id, AccountingReasonRequest request, ICommandHandler<ReopenFiscalYearCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new ReopenFiscalYearCommand(id, request.Reason ?? "", request.RowVersion), ct)))
            .RequireAuthorization(AccountingPermissions.PeriodsReopen)
            .WithName("ReopenFiscalYear")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        var periods = app.MapGroup(PeriodsRoute).WithTags("FiscalYears");

        periods.MapPost("/{id:guid}/close", (Guid id, ICommandHandler<CloseFiscalPeriodCommand, FiscalPeriodClosedResponse> handler,
                    CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new CloseFiscalPeriodCommand(id), ct)))
            .RequireAuthorization(AccountingPermissions.PeriodsManage)
            .WithName("CloseFiscalPeriod")
            .Produces<FiscalPeriodClosedResponse>()
            .CommandProblems();

        periods.MapPost("/{id:guid}/reopen", (Guid id, AccountingNoteRequest request, ICommandHandler<ReopenFiscalPeriodCommand, Done> handler,
                    CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new ReopenFiscalPeriodCommand(id, request.Reason ?? ""), ct)))
            .RequireAuthorization(AccountingPermissions.PeriodsReopen)
            .WithName("ReopenFiscalPeriod")
            .Produces(StatusCodes.Status204NoContent)
            .CommandProblems();

        return app;
    }
}
