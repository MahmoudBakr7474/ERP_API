using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Sales;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;
using WarehouseERP.Pos.Application.Contracts.Returns;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Returns.CreatePosReturn;

// A return is one transaction, like a checkout: the key is locked and looked up (a retry gets the first answer), the
// caller's open shift at the terminal is locked (cash refunds come out of its drawer), then the sale (two returns of one
// sale queue here, so together they never take back more than it sold: a database check backs it), the return window is
// checked, each line takes its cumulative share of the sale line at the price it was sold for, the refunds are checked
// (exactly what the goods are worth, each method at most what was paid that way and not refunded yet), the return is
// numbered, and Inventory posts a PosReturn document into the selling location at the cost the goods left with. pos.returns.manage
// is checked by the endpoint; the sale may be the user's own (no second person needed).
public sealed partial class CreatePosReturnHandler(
    ITransactionRunner transactions, TillAccess tills, IdempotentRequests requests, IPosShiftRepository shifts,
    IPosSaleRepository sales, IPosReturnRepository returns, IPosReturnReadStore reads, IPosSettingsRepository settings,
    PosReferences references, DocumentNumbering numbering, IPosStockPosting stock, ICurrentUser currentUser, TimeProvider clock,
    ILogger<CreatePosReturnHandler> logger)
    : ICommandHandler<CreatePosReturnCommand, PosReturnResponse>
{
    private const string Operation = "CreatePosReturn";

    public async Task<Result<PosReturnResponse>> HandleAsync(CreatePosReturnCommand command, CancellationToken ct)
    {
        var terminal = await tills.UsableAsync(command.TerminalId, ct);
        if (terminal.IsFailure)
            return terminal.Error;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        return await transactions.RunAsync(token => ReturnAsync(command, terminal.Value, company.Value, token), ct);
    }

    private async Task<Result<PosReturnResponse>> ReturnAsync(
        CreatePosReturnCommand command, PosTerminal terminal, CompanyInfo company, CancellationToken ct)
    {
        var userId = tills.UserId;
        var fingerprint = RequestFingerprint.Of(Operation, command with { IdempotencyKey = Guid.Empty }, userId);
        var previous = await requests.BeginAsync(command.IdempotencyKey, fingerprint, ct);
        if (previous.IsFailure)
            return previous.Error;
        if (previous.Value is { } recordedId)
            return await ReceiptAsync(recordedId, ct);

        var shift = await shifts.GetOpenForUpdateAsync(terminal.Id, ct);
        if (shift is null)
            return ShiftErrors.NoOpenShift;
        if (shift.CashierUserId != userId)
            return AccessErrors.NotShiftCashier;
        var sale = await sales.GetForUpdateAsync(command.SaleId, ct);
        var access = PosAccess.Check(currentUser, sale?.WarehouseId, ReturnErrors.SaleNotFound);
        if (access.IsFailure)
            return access.Error;
        var window = (await settings.GetAsync(ct)).ReturnWindowDays;
        if (references.Today(company).DayNumber - PosReferences.DateOf(sale!.SoldAtUtc, company).DayNumber > window)
            return ReturnErrors.WindowClosed(window);

        var shares = sale.TakeBack(command.Lines.Select(l => (l.SaleLineId, l.Quantity)).ToList());
        if (shares.IsFailure)
            return shares.Error;
        var number = await numbering.NextAsync(PosDocumentType.Return, terminal, ct);
        if (number.IsFailure)
            return number.Error;
        var now = clock.GetUtcNow().UtcDateTime;
        var created = PosReturn.Create(number.Value, sale, shift, terminal, command.Reason, command.Notes,
            shares.Value.Select((s, i) => new PosReturnLineInput(s, command.Lines[i].Condition)).ToList(),
            command.Refunds.Select(r => new PosPaymentInput(r.Method, r.Amount, r.Reference, r.CardLast4)).ToList(), now);
        if (created.IsFailure)
            return created.Error;
        var posReturn = created.Value;
        returns.Add(posReturn);

        var lines = posReturn.Lines.OrderBy(l => l.LineNumber).ToList();
        var posted = await stock.PostReturnAsync(new PosReturnStockRequest(posReturn.Id, posReturn.Number, terminal.WarehouseId,
            posReturn.Reason,
            lines.Select(l => new PosReturnStockLine(l.SoldStockLineId, l.ProductId, l.ProductUnitId, l.Quantity, terminal.SellingLocationId))
                .ToList()), ct);
        if (!posted.Succeeded)
            return StockErrors.Refused(posted);
        posReturn.MarkPosted(posted.DocumentId!.Value, posted.Number!, posted.Lines.Select(l => (l.StockLineId, l.CostAmount)).ToList());
        requests.Complete(command.IdempotencyKey, Operation, fingerprint, posReturn.Id, userId, now);

        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ReturnRecorded(logger, posReturn.Id, posReturn.Number, sale.Number, posReturn.TotalAmount, posted.Number!);
        return await ReceiptAsync(posReturn.Id, ct);
    }

    private async Task<Result<PosReturnResponse>> ReceiptAsync(Guid returnId, CancellationToken ct) =>
        (await reads.GetAsync(returnId, ct) ?? throw new InvalidOperationException($"Return {returnId} was recorded but can't be read back."))
        with { IsCopy = false, CopyNumber = null };

    [LoggerMessage(EventId = 8040, Level = LogLevel.Information,
        Message = "POS return {ReturnId} ({Number}) of sale {SaleNumber} refunded {TotalAmount}, stock document {StockDocumentNumber}")]
    private static partial void ReturnRecorded(ILogger logger, Guid returnId, string number, string saleNumber, decimal totalAmount, string stockDocumentNumber);
}
