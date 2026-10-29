using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.CardIncidents;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Sales;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;
using WarehouseERP.Pos.Application.Contracts.Sales;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Domain;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Sales.CheckoutPosSale;

// A checkout is one transaction: the request's key is locked and looked up (a retry gets the first receipt back), the
// caller's open shift at the terminal is locked (its cashier only; checkouts of one till queue here), the cart is priced
// by Sales, the customer checked, the payments checked and added to the shift, the receipt numbered, and Inventory posts a
// PosSale document from unreserved stock in the selling location (warehouse lock, count freezes, no negative stock, the
// moving average). The sale, its payments, the stock document, the shift's totals, the receipt number and the idempotency
// record commit together or not at all: a refused or failed checkout leaves no sale, no stock movement and no payment.
//
// A card charge happens on a standalone card terminal before the checkout, outside this transaction. When a checkout with
// card payments is refused, POS records a card incident on its own afterwards (its references, why it was refused) and
// answers Pos.Checkout.CardReversalRequired: the charge must be reversed on the card terminal. A later retry with the same
// key that records the sale after all marks the incident SaleRecorded.
public sealed partial class CheckoutPosSaleHandler(
    ITransactionRunner transactions, TillAccess tills, IdempotentRequests requests, IPosShiftRepository shifts,
    IPosShiftReadStore shiftReads, IPosSaleRepository sales, IPosSaleReadStore reads, ICardIncidentRepository incidents,
    CartPricing cart, ICustomerDirectory customers, DocumentNumbering numbering, IPosStockPosting stock, TimeProvider clock,
    ILogger<CheckoutPosSaleHandler> logger)
    : ICommandHandler<CheckoutPosSaleCommand, PosSaleResponse>
{
    private const string Operation = "CheckoutPosSale";

    public async Task<Result<PosSaleResponse>> HandleAsync(CheckoutPosSaleCommand command, CancellationToken ct)
    {
        var terminal = await tills.AccessibleAsync(command.TerminalId, ct);
        if (terminal.IsFailure)
            return terminal.Error;
        var checkedOut = await transactions.RunAsync(token => CheckoutAsync(command, terminal.Value, token), ct);
        if (checkedOut.IsSuccess || checkedOut.Error == IdempotencyErrors.Busy
            || command.Payments.All(p => p.Method != PosPaymentMethod.Card))
            return checkedOut;
        return await CardReversalRequiredAsync(command, terminal.Value, checkedOut.Error, ct);
    }

    private async Task<Result<PosSaleResponse>> CheckoutAsync(CheckoutPosSaleCommand command, PosTerminal terminal, CancellationToken ct)
    {
        var userId = tills.UserId;
        var fingerprint = RequestFingerprint.Of(Operation, command with { IdempotencyKey = Guid.Empty }, userId);
        var previous = await requests.BeginAsync(command.IdempotencyKey, fingerprint, ct);
        if (previous.IsFailure)
            return previous.Error;
        if (previous.Value is { } recordedId)
            return await ReceiptAsync(recordedId, ct);

        if (!terminal.IsActive)
            return TerminalErrors.NotActive;
        var shift = await shifts.GetOpenForUpdateAsync(terminal.Id, ct);
        if (shift is null)
            return ShiftErrors.NoOpenShift;
        if (shift.CashierUserId != userId)
            return AccessErrors.NotShiftCashier;
        var priced = await cart.PriceAsync(command.Lines, ct);
        if (priced.IsFailure)
            return priced.Error;
        var customer = await CustomerAsync(command, ct);
        if (customer.IsFailure)
            return customer.Error;
        var number = await numbering.NextAsync(PosDocumentType.Sale, terminal, ct);
        if (number.IsFailure)
            return number.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        var created = PosSale.Create(number.Value, shift, terminal, customer.Value, priced.Value.CurrencyCode, priced.Value.CurrencyDecimals,
            priced.Value.Lines, command.Payments.Select(p => new PosPaymentInput(p.Method, p.Amount, p.Reference, p.CardLast4)).ToList(),
            command.Notes, now);
        if (created.IsFailure)
            return created.Error;
        var sale = created.Value;
        sales.Add(sale);

        var lines = sale.Lines.OrderBy(l => l.LineNumber).ToList();
        var posted = await stock.PostSaleAsync(new PosSaleStockRequest(sale.Id, sale.Number, terminal.WarehouseId, null,
            lines.Select(l => new PosSaleStockLine(l.ProductId, l.ProductUnitId, l.Quantity, terminal.SellingLocationId)).ToList()), ct);
        if (!posted.Succeeded)
            return StockErrors.Refused(posted);
        sale.MarkPosted(posted.DocumentId!.Value, posted.Number!, posted.Lines.Select(l => (l.StockLineId, l.CostAmount)).ToList());
        requests.Complete(command.IdempotencyKey, Operation, fingerprint, sale.Id, userId, now);
        (await incidents.FindByKeyAsync(command.IdempotencyKey, ct))?.MarkSaleRecorded(sale.Id, now);

        var saved = await sales.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        SaleCheckedOut(logger, sale.Id, sale.Number, terminal.Code, sale.TotalAmount, posted.Number!);
        return await ReceiptAsync(sale.Id, ct);
    }

    // A registered customer must be active and is printed as Sales has them; a walk-in may give a name and a phone.
    private async Task<Result<PosSaleCustomer>> CustomerAsync(CheckoutPosSaleCommand command, CancellationToken ct)
    {
        if (command.CustomerId is not { } customerId)
            return PosSaleCustomer.WalkIn(command.CustomerName, command.CustomerPhone);
        var customer = await customers.FindAsync(customerId, ct);
        if (customer is null)
            return SaleErrors.CustomerNotFound;
        if (!customer.IsActive)
            return SaleErrors.CustomerNotActive;
        return new PosSaleCustomer(customer.Id, customer.NameAr, customer.Mobile, customer.TaxRegistrationNumber);
    }

    // The receipt the checkout prints: the original, never a copy (also when a retry gets it back).
    private async Task<Result<PosSaleResponse>> ReceiptAsync(Guid saleId, CancellationToken ct) =>
        (await reads.GetAsync(saleId, ct) ?? throw new InvalidOperationException($"Sale {saleId} was recorded but can't be read back."))
        with { IsCopy = false, CopyNumber = null };

    // Recorded in its own transaction after the checkout rolled back: the card references must survive the refusal.
    private async Task<Result<PosSaleResponse>> CardReversalRequiredAsync(
        CheckoutPosSaleCommand command, PosTerminal terminal, Error refusal, CancellationToken ct)
    {
        var cards = command.Payments.Where(p => p.Method == PosPaymentMethod.Card).ToList();
        var references = string.Join("; ", cards.Select(p => string.IsNullOrWhiteSpace(p.CardLast4)
            ? (p.Reference ?? "").Trim()
            : $"{(p.Reference ?? "").Trim()} (*{p.CardLast4.Trim()})"));
        var amount = cards.Sum(p => p.Amount);
        var userId = tills.UserId;
        var shiftId = await shiftReads.FindOpenAsync(terminal.Id, userId, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var recorded = await transactions.RunAsync<Guid>(async token =>
        {
            var incident = await incidents.FindByKeyAsync(command.IdempotencyKey, token);
            if (incident is null)
            {
                incident = PosCardIncident.Record(command.IdempotencyKey, terminal, shiftId, userId, amount, references, refusal.Code,
                    refusal.Message, now);
                incidents.Add(incident);
            }
            else
                incident.Again(amount, references, refusal.Code, refusal.Message, now);
            var saved = await incidents.SaveChangesAsync(token);
            return saved.IsFailure ? saved.Error : incident.Id;
        }, ct);

        if (recorded.IsSuccess)
            CardIncidentRecorded(logger, recorded.Value, terminal.Code, amount, refusal.Code);
        else
            CardIncidentNotRecorded(logger, terminal.Code, amount, refusal.Code, recorded.Error.Code);
        return SaleErrors.CardReversalRequired(refusal, references, recorded.IsSuccess ? recorded.Value : null);
    }

    [LoggerMessage(EventId = 8030, Level = LogLevel.Information,
        Message = "POS sale {SaleId} ({Number}) checked out at terminal {TerminalCode} for {TotalAmount}, stock document {StockDocumentNumber}")]
    private static partial void SaleCheckedOut(ILogger logger, Guid saleId, string number, string terminalCode, decimal totalAmount, string stockDocumentNumber);

    [LoggerMessage(EventId = 8031, Level = LogLevel.Warning,
        Message = "Card incident {IncidentId}: checkout at terminal {TerminalCode} refused ({ErrorCode}) after card payments of {CardAmount}")]
    private static partial void CardIncidentRecorded(ILogger logger, Guid incidentId, string terminalCode, decimal cardAmount, string errorCode);

    [LoggerMessage(EventId = 8032, Level = LogLevel.Error,
        Message = "Checkout at terminal {TerminalCode} refused ({ErrorCode}) after card payments of {CardAmount}, and the card incident could not be saved ({SaveError})")]
    private static partial void CardIncidentNotRecorded(ILogger logger, string terminalCode, decimal cardAmount, string errorCode, string saveError);
}
