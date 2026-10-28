using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.DocumentPolicies;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.CreatePurchaseOrder;

// A draft order to an active supplier for one physical warehouse in the user's scope, in the company's base currency.
// It copies the order approval policy now. Lines that name purchase request lines take their quantities from them in the
// same transaction.
public sealed partial class CreatePurchaseOrderHandler(
    ITransactionRunner transactions, IPurchaseOrderRepository orders, IPurchaseRequestRepository requests,
    IDocumentPolicyRepository policies, PurchasingReferences references, ICurrentUser currentUser, TimeProvider clock,
    ILogger<CreatePurchaseOrderHandler> logger)
    : ICommandHandler<CreatePurchaseOrderCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreatePurchaseOrderCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return AccessErrors.WarehouseDenied;
        return await transactions.RunAsync(token => CreateAsync(command, token), ct);
    }

    private async Task<Result<Guid>> CreateAsync(CreatePurchaseOrderCommand command, CancellationToken ct)
    {
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var warehouse = await references.UsableWarehouseAsync(command.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        var supplier = await references.ActiveSupplierAsync(command.SupplierId, ct);
        if (supplier.IsFailure)
            return supplier.Error;
        var lines = await OrderLines.ResolveAsync(references, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;

        var requiresApproval = await policies.RequiresApprovalAsync(PurchasingDocumentType.PurchaseOrder, ct);
        var terms = new OrderTerms(command.SupplierId, command.ExpectedDeliveryDate, command.SupplierReference,
            command.PaymentTermDays ?? supplier.Value.PaymentTermDays, command.Notes);
        var created = PurchaseOrder.CreateDraft(command.WarehouseId, warehouse.Value.BranchId, company.Value.BaseCurrencyCode,
            requiresApproval, terms, lines.Value, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        var drawn = await RequestDraws.MoveAsync(requests, command.WarehouseId, new Dictionary<Guid, RequestDraw>(),
            RequestDraws.Of(created.Value.Lines), ct);
        if (drawn.IsFailure)
            return drawn.Error;

        orders.Add(created.Value);
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        OrderCreated(logger, created.Value.Id, command.SupplierId);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 6010, Level = LogLevel.Information, Message = "Purchase order {OrderId} drafted for supplier {SupplierId}")]
    private static partial void OrderCreated(ILogger logger, Guid orderId, Guid supplierId);
}
