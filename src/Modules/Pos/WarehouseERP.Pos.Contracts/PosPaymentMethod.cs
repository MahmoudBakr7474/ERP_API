namespace WarehouseERP.Pos.Contracts;

/// <summary>How a POS sale is paid or a return refunded.</summary>
public enum PosPaymentMethod : byte
{
    /// <summary>Cash in the shift's drawer.</summary>
    Cash = 1,

    /// <summary>A card charged or refunded on a standalone card terminal; POS records its authorization reference only.</summary>
    Card = 2,
}
