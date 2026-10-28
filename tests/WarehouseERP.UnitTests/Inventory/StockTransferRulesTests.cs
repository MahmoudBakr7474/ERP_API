using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.UnitTests.Inventory;

// A transfer's life: between two warehouses, dispatched directly or after someone else approves, received in parts,
// and closed by a loss for what never arrives. The ledger documents it produces cannot be edited or reversed.
public sealed class StockTransferRulesTests
{
    private static readonly Guid Source = Guid.NewGuid();
    private static readonly Guid Destination = Guid.NewGuid();
    private static readonly Guid Transit = Guid.NewGuid();
    private static readonly Guid Branch = Guid.NewGuid();
    private static readonly Guid SourceLocation = Guid.NewGuid();
    private static readonly Guid TransitLocation = Guid.NewGuid();
    private static readonly Guid DestinationLocation = Guid.NewGuid();
    private static readonly Guid Creator = Guid.NewGuid();
    private static readonly Guid Approver = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    private static TransferLineInput Line(decimal quantity, decimal factor = 1m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), factor, UnitAllowsFraction: false, BaseUnitAllowsFraction: false, quantity,
            SourceLocation);

    // EF Core gives ids and the creator on save; the rules tests set them the same way.
    private static StockTransfer Draft(bool requiresApproval, params TransferLineInput[] lines)
    {
        var transfer = StockTransfer.CreateDraft(Source, Branch, Destination, Branch, Transit, requiresApproval, " EXT ", null, lines,
            Creator, Now).Value;
        typeof(StockTransfer).GetProperty(nameof(StockTransfer.Id))!.SetValue(transfer, Guid.NewGuid());
        typeof(StockTransfer).GetProperty(nameof(StockTransfer.CreatedByUserId))!.SetValue(transfer, Creator);
        foreach (var line in transfer.Lines)
            typeof(StockTransferLine).GetProperty(nameof(StockTransferLine.Id))!.SetValue(line, Guid.NewGuid());
        return transfer;
    }

    private static StockTransfer Dispatched(params TransferLineInput[] lines)
    {
        var transfer = Draft(false, lines);
        transfer.MarkDispatched("TRF-B-1-2026-000001", TransitLocation, byApproval: false, Creator, Now);
        return transfer;
    }

    private static TransferReceiptInput Arrived(StockTransferLine line, decimal quantity) =>
        new(line.Id, quantity, DestinationLocation, Guid.NewGuid(), Guid.NewGuid(), BaseUnitAllowsFraction: false);

    [Fact]
    public void A_transfer_goes_from_one_warehouse_to_another_in_base_units()
    {
        Assert.Equal("Inventory.StockTransfer.SameWarehouse", StockTransfer.CreateDraft(Source, Branch, Source, Branch, Transit, false,
            null, null, [Line(1m)], Creator, Now).Error?.Code);
        Assert.Equal("Inventory.StockTransfer.NoLines", StockTransfer.CreateDraft(Source, Branch, Destination, Branch, Transit, false,
            null, null, [], Creator, Now).Error?.Code);
        Assert.Equal("Inventory.StockDocument.QuantityMustBeWhole", StockTransfer.CreateDraft(Source, Branch, Destination, Branch,
            Transit, false, null, null, [Line(1.5m)], Creator, Now).Error?.Code);

        var transfer = Draft(false, Line(2m, factor: 12m));

        var line = Assert.Single(transfer.Lines);
        Assert.Equal((2m, 24m, 24m, SourceLocation), (line.Quantity, line.BaseQuantity, line.OutstandingQuantity, line.SourceLocationId));
        Assert.Equal((StockTransferStatus.Draft, "EXT", (string?)null), (transfer.Status, transfer.ExternalReference, transfer.Number));
        Assert.False(transfer.IsOpen);
    }

    [Fact]
    public void Dispatch_waits_for_an_approver_who_did_not_create_the_transfer_when_the_policy_says_so()
    {
        var transfer = Draft(true, Line(5m));

        Assert.Equal("Inventory.StockTransfer.ApprovalRequired", transfer.CanDispatch(byApproval: false, Creator).Error?.Code);
        Assert.Equal("Inventory.StockTransfer.NotSubmitted", transfer.CanDispatch(byApproval: true, Approver).Error?.Code);
        Assert.True(transfer.Submit(Creator, Now).IsSuccess);
        Assert.True(transfer.IsOpen);
        Assert.Equal("Inventory.StockTransfer.SelfApproval", transfer.CanDispatch(byApproval: true, Creator).Error?.Code);
        Assert.Equal("Inventory.StockTransfer.SelfApproval", transfer.CanDispatch(byApproval: true, null).Error?.Code);
        Assert.Throws<InvalidOperationException>(() => transfer.MarkDispatched("TRF-1", TransitLocation, byApproval: true, Creator, Now));

        transfer.MarkDispatched("TRF-1", TransitLocation, byApproval: true, Approver, Now);

        Assert.Equal((StockTransferStatus.InTransit, "TRF-1", TransitLocation, Approver, Approver),
            (transfer.Status, transfer.Number, transfer.TransitLocationId, transfer.ApprovedByUserId, transfer.DispatchedByUserId));
        Assert.Equal([StockTransferAction.Created, StockTransferAction.Submitted, StockTransferAction.Approved, StockTransferAction.Dispatched],
            transfer.Events.Select(e => e.Action));
        Assert.Equal("Inventory.StockTransfer.NotOpen", transfer.Cancel(Creator, Now).Error?.Code);
    }

    [Fact]
    public void A_transfer_without_approval_is_dispatched_directly_and_a_rejection_sends_a_submitted_one_back()
    {
        var direct = Draft(false, Line(1m));
        Assert.Equal("Inventory.StockTransfer.ApprovalNotRequired", direct.Submit(Creator, Now).Error?.Code);
        Assert.True(direct.CanDispatch(byApproval: false, Creator).IsSuccess);

        var approved = Draft(true, Line(1m));
        approved.Submit(Creator, Now);
        Assert.True(approved.Reject("wrong lines", Approver, Now).IsSuccess);
        Assert.Equal(StockTransferStatus.Draft, approved.Status);
        Assert.True(approved.ReplaceDraft(Transit, null, null, [Line(3m)]).IsSuccess);
        Assert.Equal(3m, Assert.Single(approved.Lines).BaseQuantity);
        Assert.True(approved.Cancel(Creator, Now).IsSuccess);
        Assert.Equal("Inventory.StockTransfer.NotDraft", approved.ReplaceDraft(Transit, null, null, [Line(1m)]).Error?.Code);
    }

    [Fact]
    public void Partial_receipts_leave_the_rest_in_transit_until_everything_arrived()
    {
        var transfer = Dispatched(Line(10m), Line(5m));
        var (first, second) = (transfer.Lines[0], transfer.Lines[1]);

        Assert.Equal("Inventory.StockTransfer.NothingToReceive", transfer.CheckReceipt([]).Error?.Code);
        Assert.Equal("Inventory.StockTransfer.UnknownLine",
            transfer.CheckReceipt([Arrived(first, 1m) with { LineId = Guid.NewGuid() }]).Error?.Code);
        Assert.Equal("Inventory.StockTransfer.QuantityMustBeWhole", transfer.CheckReceipt([Arrived(first, 0.5m)]).Error?.Code);
        Assert.Equal("Inventory.StockTransfer.ReceiptExceedsOutstanding",
            transfer.CheckReceipt([Arrived(first, 6m), Arrived(first, 5m)]).Error?.Code);

        transfer.RecordReceipt([Arrived(first, 4m)], "TRR-1", Approver, Now);
        Assert.Equal((StockTransferStatus.PartiallyReceived, 6m, 5m, 11m),
            (transfer.Status, first.OutstandingQuantity, second.OutstandingQuantity, transfer.OutstandingQuantity));
        Assert.Null(transfer.CompletedAtUtc);

        transfer.RecordReceipt([Arrived(first, 6m), Arrived(second, 5m)], "TRR-2", Approver, Now);
        Assert.Equal((StockTransferStatus.Received, 0m, Now), (transfer.Status, transfer.OutstandingQuantity, transfer.CompletedAtUtc));
        Assert.False(transfer.IsOpen);
        Assert.Equal("Inventory.StockTransfer.NotInTransit", transfer.CheckReceipt([Arrived(first, 1m)]).Error?.Code);
        Assert.Equal("Inventory.StockTransfer.NotInTransit", transfer.BeginClosing(null, Creator, Now).Error?.Code);
    }

    [Fact]
    public void What_never_arrives_is_written_off_by_closing_and_nothing_is_received_meanwhile()
    {
        var transfer = Dispatched(Line(10m), Line(5m));
        var (first, second) = (transfer.Lines[0], transfer.Lines[1]);
        transfer.RecordReceipt([Arrived(first, 7m)], "TRR-1", Approver, Now);

        Assert.True(transfer.BeginClosing("lost on the road", Creator, Now).IsSuccess);
        Assert.Equal(StockTransferStatus.Closing, transfer.Status);
        Assert.Equal("Inventory.StockTransfer.NotInTransit", transfer.CheckReceipt([Arrived(second, 1m)]).Error?.Code);

        transfer.AbortClosing(Creator, Now);
        Assert.Equal(StockTransferStatus.PartiallyReceived, transfer.Status);
        transfer.BeginClosing(null, Creator, Now);

        transfer.CompleteClosing("ADJ-1", Approver, Now);

        Assert.Equal((StockTransferStatus.Closed, 0m), (transfer.Status, transfer.OutstandingQuantity));
        Assert.Equal((7m, 3m), (first.ReceivedQuantity, first.LostQuantity));
        Assert.Equal((0m, 5m), (second.ReceivedQuantity, second.LostQuantity));
        Assert.Equal("ADJ-1", transfer.Events[^1].Note);
        Assert.Throws<InvalidOperationException>(() => transfer.CompleteClosing("ADJ-2", Approver, Now));
    }

    [Fact]
    public void A_fully_received_transfer_has_nothing_to_close()
    {
        var transfer = Dispatched(Line(2m));
        transfer.RecordReceipt([Arrived(transfer.Lines[0], 2m)], "TRR-1", Approver, Now);

        Assert.Equal(StockTransferStatus.Received, transfer.Status);
        Assert.Equal("Inventory.StockTransfer.NotInTransit", transfer.BeginClosing(null, Creator, Now).Error?.Code);
    }

    [Fact]
    public void The_dispatch_and_receipt_documents_move_base_units_through_the_transfers_transit_location()
    {
        var transfer = Dispatched(Line(2m, factor: 12m));
        var line = transfer.Lines[0];

        var dispatch = StockDocument.CreateTransferDispatch(transfer, TransitLocation, Creator, Now);
        var moved = Assert.Single(dispatch.Lines);
        Assert.Equal((StockDocumentType.TransferDispatch, Source, transfer.Id), (dispatch.DocumentType, dispatch.WarehouseId, dispatch.TransferId));
        Assert.Equal((StockDirection.Move, 2m, 24m), (moved.Direction, moved.Quantity, moved.BaseQuantity));
        var allocation = Assert.Single(moved.Allocations);
        Assert.Equal((Source, SourceLocation, Transit, TransitLocation, 24m),
            (allocation.FromWarehouseId!.Value, allocation.FromLocationId!.Value, allocation.ToWarehouseId!.Value,
                allocation.ToLocationId!.Value, allocation.BaseQuantity));

        var receipt = StockDocument.CreateTransferReceipt(transfer, [Arrived(line, 5m)], Approver, Now);
        var arrived = Assert.Single(Assert.Single(receipt.Lines).Allocations);
        Assert.Equal((StockDocumentType.TransferReceipt, Destination), (receipt.DocumentType, receipt.WarehouseId));
        Assert.Equal((Transit, TransitLocation, Destination, DestinationLocation, 5m),
            (arrived.FromWarehouseId!.Value, arrived.FromLocationId!.Value, arrived.ToWarehouseId!.Value, arrived.ToLocationId!.Value,
                arrived.BaseQuantity));
    }

    [Fact]
    public void Transfer_documents_cannot_be_edited_or_reversed_and_the_loss_waits_for_approval()
    {
        var transfer = Dispatched(Line(3m));
        var dispatch = StockDocument.CreateTransferDispatch(transfer, TransitLocation, Creator, Now);
        Assert.Equal("Inventory.StockDocument.LinkedLinesFixed",
            dispatch.ReplaceDraft(AdjustmentReason.Damage, null, null, []).Error?.Code);
        dispatch.MarkPostedGenerated("TRF-1", 2026, 1, approvedBy: null, Creator, Now);
        Assert.Equal("Inventory.StockDocument.NotReversible", StockDocument.CreateReversal(dispatch, null, Approver, Now).Error?.Code);

        var loss = StockDocument.CreateTransferLoss(transfer, Branch, AdjustmentReason.Loss, "never arrived",
            [new GeneratedLineInput(transfer.Lines[0].ProductId, Guid.NewGuid(), Guid.NewGuid(), 3m, StockDirection.Out, null,
                new StockPlace(Transit, TransitLocation))], Creator, Now);
        Assert.Equal((StockDocumentType.Adjustment, StockDocumentStatus.Submitted, true, Transit, transfer.Id),
            (loss.DocumentType, loss.Status, loss.RequiresApproval, loss.WarehouseId, loss.TransferId));
        loss.MarkPostedGenerated("ADJ-1", 2026, 1, approvedBy: Approver, Approver, Now);
        Assert.Equal("Inventory.StockDocument.NotReversible", StockDocument.CreateReversal(loss, null, Creator, Now).Error?.Code);
    }
}
