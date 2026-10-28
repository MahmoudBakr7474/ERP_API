using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Features.StockDocuments.CreateStockDocument;
using WarehouseERP.Inventory.Application.Features.StockDocuments.PostStockDocument;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.UnitTests.Inventory;

public sealed class StockDocumentValidatorTests
{
    private static CreateStockDocumentCommand Receipt(params StockLineRequest[] lines) =>
        new(StockDocumentType.Receipt, Guid.NewGuid(), null, null, null, lines);

    [Fact]
    public void A_well_formed_draft_passes()
    {
        var result = new CreateStockDocumentValidator().Validate(Receipt(new StockLineRequest(Guid.NewGuid(), Guid.NewGuid(), 2.5m, null, 12.12345678m)));

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Line_shapes_are_checked_field_by_field()
    {
        var result = new CreateStockDocumentValidator().Validate(Receipt(
            new StockLineRequest(Guid.Empty, Guid.NewGuid(), 1m),
            new StockLineRequest(Guid.NewGuid(), Guid.NewGuid(), 1.23456m),
            new StockLineRequest(Guid.NewGuid(), Guid.NewGuid(), 0m),
            new StockLineRequest(Guid.NewGuid(), Guid.NewGuid(), 1m, Guid.Empty, 1.123456789m)));

        Assert.Equal(
            ["Lines[0].ProductId", "Lines[1].Quantity", "Lines[2].Quantity", "Lines[3].LocationId", "Lines[3].UnitCost"],
            result.Errors.Select(e => e.PropertyName).Order());
    }

    [Fact]
    public void A_reversal_is_never_drafted_and_a_draft_needs_lines()
    {
        var result = new CreateStockDocumentValidator().Validate(new CreateStockDocumentCommand(
            StockDocumentType.Reversal, Guid.NewGuid(), null, null, null, []));

        Assert.Equal(["DocumentType", "Lines"], result.Errors.Select(e => e.PropertyName).Order());
    }

    [Fact]
    public void Posting_needs_an_idempotency_key_and_the_row_version_read()
    {
        var result = new PostStockDocumentValidator().Validate(new PostStockDocumentCommand(Guid.NewGuid(), Guid.Empty, "not-base64"));

        Assert.Equal(["IdempotencyKey", "RowVersion"], result.Errors.Select(e => e.PropertyName).Order());
    }
}
