namespace WarehouseERP.Pos.Contracts;

/// <summary>What POS knows about a payment or a refund it recorded.</summary>
public enum PosPaymentStatus : byte
{
    /// <summary>Cash that changed hands at the drawer.</summary>
    Settled = 1,

    /// <summary>
    /// Approved on a standalone card terminal and confirmed by the cashier with its reference. POS has no link to the
    /// payment provider: settlement is reconciled outside POS (V1).
    /// </summary>
    ExternallyConfirmed = 2,
}
