namespace WarehouseERP.Pos.Domain;

/// <summary>What one returned quantity takes back from its sale line, at the price, discount and VAT it was sold with.</summary>
public sealed record ReturnShare(PosSaleLine Line, decimal Quantity, decimal NetAmount, decimal TaxAmount)
{
    public decimal TotalAmount => NetAmount + TaxAmount;
}
