using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Orders;

// What confirming an order needs, whether it is confirmed on submission or by an approver, inside the caller's transaction
// with the order locked: an active customer, a credit hold the settings don't block, a warehouse that still sells, then
// its number, and Inventory reserving every line in base units or nothing. A refusal leaves the caller to roll back, so a
// failed confirmation keeps neither a number nor a partial reservation.
public sealed class OrderConfirmation(
    SalesReferences references, ISalesSettingsRepository settings, DocumentNumbering numbering, IStockReservations reservations)
{
    /// <summary>
    /// Whether the order may be confirmed now: what refuses it, or why it needs an approver first (empty: none). The
    /// customer's hold refuses it when the settings block confirmation, and asks for approval when they ask for one.
    /// </summary>
    public async Task<Result<IReadOnlyList<ApprovalReason>>> CheckAsync(SalesOrder order, bool approving, CancellationToken ct)
    {
        var customer = await references.ActiveCustomerAsync(order.CustomerId, ct);
        if (customer.IsFailure)
            return customer.Error;
        var policies = await settings.GetAsync(ct);
        if (customer.Value.IsOnCreditHold && policies.CreditHoldPolicy == CreditHoldPolicy.BlockConfirmation)
            return CustomerErrors.OnCreditHold;
        var warehouse = await references.SellingWarehouseAsync(order.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        IReadOnlyList<ApprovalReason> reasons = approving ? [] : policies.ApprovalReasons(order.Lines, isOrder: true, customer.Value.IsOnCreditHold);
        return Result.Success(reasons);
    }

    /// <summary>Numbers the order (directly, or by the approver) and reserves its stock.</summary>
    public async Task<Result> ConfirmAsync(SalesOrder order, bool approving, Guid? userId, DateTime nowUtc, CancellationToken ct)
    {
        var number = await numbering.NextAsync(SalesDocumentType.Order, order.BranchId, ct);
        if (number.IsFailure)
            return number.Error;
        var confirmed = approving ? order.Approve(number.Value, userId, nowUtc) : order.ConfirmDirectly(number.Value, userId, nowUtc);
        if (confirmed.IsFailure)
            return confirmed;

        var reserved = await reservations.ReserveAsync(new StockReservationRequest(
            order.Id, number.Value.Number, order.WarehouseId,
            order.Lines.OrderBy(l => l.LineNumber)
                .Select(l => new StockReservationLine(l.Id, l.ProductId, l.ProductUnitId, l.Quantity))
                .ToList()), ct);
        return reserved.Succeeded ? Result.Success() : StockErrors.Refused(reserved);
    }
}
