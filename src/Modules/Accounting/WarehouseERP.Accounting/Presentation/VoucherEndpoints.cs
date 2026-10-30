using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.Accounting.Application.Features.Vouchers.CreateVoucher;
using WarehouseERP.Accounting.Application.Features.Vouchers.UpdateVoucher;
using WarehouseERP.Accounting.Presentation.Contracts.Vouchers;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;

namespace WarehouseERP.Accounting.Presentation;

// Vouchers are journals whose lines are built from what they say. Drafting and changing one needs the permission of its
// type — receivables (customer receipts and refunds), payables (supplier payments, refunds, expense bills) or treasury
// (cash vouchers, transfers) — which the handler checks; submitting, posting, approving, cancelling and reversing go
// through /api/accounting/journals like any journal.
internal static class VoucherEndpoints
{
    public const string Route = "/api/accounting/vouchers";

    public static IEndpointRouteBuilder MapVoucherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Vouchers");

        group.MapPost("/", (CreateVoucherCommand command, ICommandHandler<CreateVoucherCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), JournalEndpoints.Route))
            .RequireAuthorization()
            .WithName("CreateVoucher")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdateVoucherRequest request, ICommandHandler<UpdateVoucherCommand, string> handler, CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdateVoucherCommand(id, request.PostingDate, request.Description,
                    request.Reference, request.CashAccountId, request.CounterCashAccountId, request.PartyType, request.PartyId, request.PaymentMethod,
                    request.Amount, request.WithholdingAmount, request.TaxAmount, request.DueDate, request.BranchId, request.Lines, request.Allocations,
                    request.RowVersion), ct)))
            .RequireAuthorization()
            .WithName("UpdateVoucher")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        return app;
    }
}
