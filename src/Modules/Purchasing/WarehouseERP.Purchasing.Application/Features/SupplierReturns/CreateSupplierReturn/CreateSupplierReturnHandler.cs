using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.CreateSupplierReturn;

// A draft return of goods one posted receipt brought in: each line at most what that receipt line brought and was not
// returned yet (checked again when approved, when Inventory also checks the goods are physically there). One save point.
public sealed partial class CreateSupplierReturnHandler(
    ISupplierReturnRepository returns, IGoodsReceiptRepository receipts, IPurchaseOrderRepository orders, ICurrentUser currentUser,
    TimeProvider clock, ILogger<CreateSupplierReturnHandler> logger)
    : ICommandHandler<CreateSupplierReturnCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateSupplierReturnCommand command, CancellationToken ct)
    {
        var access = DocumentAccess.Check(currentUser, await receipts.FindWarehouseAsync(command.GoodsReceiptId, ct), SupplierReturnErrors.ReceiptNotFound);
        if (access.IsFailure)
            return access.Error;
        var receipt = await receipts.GetForUpdateAsync(command.GoodsReceiptId, ct);
        if (receipt is null)
            return SupplierReturnErrors.ReceiptNotFound;
        var order = await orders.GetForUpdateAsync(receipt.PurchaseOrderId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Receipt {receipt.Id} points at order {receipt.PurchaseOrderId}, which does not exist.");

        var created = SupplierReturn.CreateDraft(receipt, order.CurrencyCode, command.Reason, command.Notes,
            ReturnLines.Inputs(command.Lines), currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        returns.Add(created.Value);
        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ReturnCreated(logger, created.Value.Id, receipt.Id);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 6040, Level = LogLevel.Information, Message = "Supplier return {ReturnId} drafted from goods receipt {ReceiptId}")]
    private static partial void ReturnCreated(ILogger logger, Guid returnId, Guid receiptId);
}
