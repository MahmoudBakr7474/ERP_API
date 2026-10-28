using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.UnitTests.Inventory;

// Stock document invariants: what a line may carry, how quantities convert, and the document's life.
public sealed class StockDocumentRulesTests
{
    private static readonly Guid Warehouse = Guid.NewGuid();
    private static readonly Guid Branch = Guid.NewGuid();
    private static readonly Guid Location = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    private static StockLineInput Line(
        decimal quantity, StockDirection direction = StockDirection.In, decimal? unitCost = 10m, decimal factor = 1m,
        bool unitAllowsFraction = false, bool baseAllowsFraction = false) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), factor, unitAllowsFraction, baseAllowsFraction, quantity, direction,
            unitCost, Location);

    private static StockDocument Draft(StockDocumentType type, bool requiresApproval, params StockLineInput[] lines) =>
        StockDocument.CreateDraft(type, Warehouse, Branch, requiresApproval,
            type == StockDocumentType.Adjustment ? AdjustmentReason.Damage : null, " REF-1 ", null, lines, Guid.NewGuid(), Now).Value;

    [Fact]
    public void A_line_converts_to_the_base_unit_exactly_and_is_allocated_to_its_location()
    {
        var document = Draft(StockDocumentType.Receipt, false, Line(2.5m, factor: 12m, unitAllowsFraction: true));

        var line = Assert.Single(document.Lines);
        Assert.Equal((2.5m, 30m, 12m), (line.Quantity, line.BaseQuantity, line.ConversionFactor));
        var allocation = Assert.Single(line.Allocations);
        Assert.Equal((Warehouse, Location, (Guid?)null, (Guid?)null, 30m),
            (allocation.ToWarehouseId!.Value, allocation.ToLocationId!.Value, allocation.FromWarehouseId, allocation.FromLocationId, allocation.BaseQuantity));
        Assert.Equal("REF-1", document.ExternalReference);
        Assert.Equal(StockDocumentStatus.Draft, document.Status);
    }

    [Theory]
    [InlineData(0.1, 12, true, false, "Inventory.StockDocument.ConversionNotExact")] // 1.2 pieces of a piece that can't be split
    [InlineData(0.25, 0.001, true, true, "Inventory.StockDocument.ConversionNotExact")] // 0.00025: more than 4 decimals
    [InlineData(1.5, 1, false, false, "Inventory.StockDocument.QuantityMustBeWhole")]
    public void A_quantity_that_does_not_fit_is_refused_never_rounded(
        double quantity, double factor, bool unitAllowsFraction, bool baseAllowsFraction, string code)
    {
        var result = StockDocument.CreateDraft(StockDocumentType.Receipt, Warehouse, Branch, false, null, null, null,
            [Line((decimal)quantity, factor: (decimal)factor, unitAllowsFraction: unitAllowsFraction, baseAllowsFraction: baseAllowsFraction)],
            null, Now);

        Assert.Equal(code, result.Error?.Code);
    }

    [Fact]
    public void Each_type_moves_stock_its_own_way_and_only_incoming_stock_carries_a_cost()
    {
        Assert.Equal("Inventory.StockDocument.UnitCostRequired", Create(StockDocumentType.Receipt, Line(1, unitCost: null)).Error?.Code);
        Assert.Equal("Inventory.StockDocument.UnitCostRequired", Create(StockDocumentType.OpeningBalance, Line(1, unitCost: null)).Error?.Code);
        Assert.Equal("Inventory.StockDocument.WrongDirection", Create(StockDocumentType.Receipt, Line(1, StockDirection.Out, null)).Error?.Code);
        Assert.Equal("Inventory.StockDocument.WrongDirection", Create(StockDocumentType.Issue, Line(1, StockDirection.In, null)).Error?.Code);
        Assert.Equal("Inventory.StockDocument.UnitCostNotAllowed", Create(StockDocumentType.Issue, Line(1, StockDirection.Out, 5m)).Error?.Code);
        Assert.Equal("Inventory.StockDocument.ReasonRequired", Create(StockDocumentType.Adjustment, Line(1, StockDirection.Out, null)).Error?.Code);
        Assert.Equal("Inventory.StockDocument.ReasonOnlyForAdjustments",
            StockDocument.CreateDraft(StockDocumentType.Receipt, Warehouse, Branch, false, AdjustmentReason.Loss, null, null, [Line(1)], null, Now).Error?.Code);
        Assert.Equal("Inventory.StockDocument.NoLines", Create(StockDocumentType.Receipt).Error?.Code);
        Assert.Equal("Inventory.StockDocument.UnitCostTooLarge", Create(StockDocumentType.Receipt, Line(1, unitCost: 1000m, factor: 0.00000001m)).Error?.Code);

        static SharedKernel.Results.Result<StockDocument> Create(StockDocumentType type, params StockLineInput[] lines) =>
            StockDocument.CreateDraft(type, Warehouse, Branch, false, null, null, null, lines, null, Now);
    }

    [Fact]
    public void A_document_without_approval_posts_from_draft_and_one_with_approval_only_after_submission()
    {
        var direct = Draft(StockDocumentType.Receipt, requiresApproval: false, Line(1));
        Assert.Equal("Inventory.StockDocument.ApprovalNotRequired", direct.Submit(null, Now).Error?.Code);
        Assert.True(direct.CanPost(byApproval: false, Guid.NewGuid()).IsSuccess);
        Assert.Equal("Inventory.StockDocument.NotSubmitted", direct.CanPost(byApproval: true, Guid.NewGuid()).Error?.Code);

        var approved = Draft(StockDocumentType.Adjustment, requiresApproval: true, Line(1, StockDirection.Out, null));
        Assert.Equal("Inventory.StockDocument.ApprovalRequired", approved.CanPost(byApproval: false, Guid.NewGuid()).Error?.Code);
        Assert.True(approved.Submit(Guid.NewGuid(), Now).IsSuccess);
        Assert.Equal("Inventory.StockDocument.NotDraft", approved.ReplaceDraft(AdjustmentReason.Loss, null, null, [Line(1, StockDirection.Out, null)]).Error?.Code);
        Assert.Equal("Inventory.StockDocument.SelfApproval", approved.CanPost(byApproval: true, userId: null).Error?.Code);
        Assert.True(approved.Reject("الكمية خطأ", Guid.NewGuid(), Now).IsSuccess);
        Assert.Equal(StockDocumentStatus.Draft, approved.Status);
        Assert.Equal([StockDocumentAction.Created, StockDocumentAction.Submitted, StockDocumentAction.Rejected], approved.Events.Select(e => e.Action));
        Assert.Equal("الكمية خطأ", approved.Events[^1].Note);
    }

    [Fact]
    public void Posting_numbers_the_document_and_makes_it_final()
    {
        var document = Draft(StockDocumentType.Issue, false, Line(3, StockDirection.Out, null));
        var poster = Guid.NewGuid();

        document.MarkPosted("ISS-B1-2026-000007", 2026, 7, byApproval: false, poster, Now);

        Assert.Equal((StockDocumentStatus.Posted, "ISS-B1-2026-000007", (short?)2026, (int?)7, (DateTime?)Now, (Guid?)poster, (Guid?)null),
            (document.Status, document.Number, document.FiscalYear, document.SequenceNumber, document.PostedAtUtc, document.PostedByUserId, document.ApprovedByUserId));
        Assert.Equal("Inventory.StockDocument.NotOpen", document.Cancel(poster, Now).Error?.Code);
        Assert.Equal("Inventory.StockDocument.NotDraft", document.ReplaceDraft(null, null, null, [Line(1, StockDirection.Out, null)]).Error?.Code);
        Assert.Throws<InvalidOperationException>(() => document.MarkPosted("ISS-B1-2026-000008", 2026, 8, false, poster, Now));
    }

    [Fact]
    public void A_reversal_mirrors_a_posted_document_and_waits_for_approval()
    {
        var original = Draft(StockDocumentType.Receipt, false, Line(4, factor: 6m), Line(1));
        Assert.Equal("Inventory.StockDocument.NotPosted", StockDocument.CreateReversal(original, null, null, Now).Error?.Code);
        original.MarkPosted("RCV-B1-2026-000001", 2026, 1, false, Guid.NewGuid(), Now);

        var reversal = StockDocument.CreateReversal(original, " wrong supplier ", Guid.NewGuid(), Now).Value;

        Assert.Equal((StockDocumentType.Reversal, StockDocumentStatus.Submitted, true, (Guid?)original.Id, "RCV-B1-2026-000001", "wrong supplier"),
            (reversal.DocumentType, reversal.Status, reversal.RequiresApproval, reversal.ReversalOfDocumentId, reversal.ExternalReference, reversal.Notes));
        Assert.All(reversal.Lines.Zip(original.Lines), pair =>
        {
            var (back, line) = pair;
            Assert.Equal((StockDirection.Out, line.BaseQuantity, (Guid?)line.Id), (back.Direction, back.BaseQuantity, back.ReversesLineId));
            var allocation = Assert.Single(back.Allocations);
            Assert.Equal((Location, (Guid?)null), (allocation.FromLocationId!.Value, allocation.ToLocationId));
        });
        Assert.Equal("Inventory.StockDocument.ReversalLinesFixed", reversal.ReplaceDraft(null, null, null, [Line(1)]).Error?.Code);
        reversal.MarkPosted("REV-B1-2026-000001", 2026, 1, byApproval: true, Guid.NewGuid(), Now);
        Assert.Equal("Inventory.StockDocument.CannotReverseReversal", StockDocument.CreateReversal(reversal, null, null, Now).Error?.Code);
    }

    [Fact]
    public void Numbers_read_type_branch_year_and_sequence()
    {
        Assert.Equal("ADJ-CAI-2026-000042", StockDocumentNumbers.Format(StockDocumentType.Adjustment, "CAI", 2026, 42));
        Assert.Equal("OPN-ALX-2027-1234567", StockDocumentNumbers.Format(StockDocumentType.OpeningBalance, "ALX", 2027, 1_234_567));
        Assert.Equal("DR-000123", StockDocumentNumbers.DraftReference(123));
    }
}
