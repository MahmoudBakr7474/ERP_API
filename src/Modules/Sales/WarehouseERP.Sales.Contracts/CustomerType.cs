namespace WarehouseERP.Sales.Contracts;

/// <summary>Who the customer is; fixed once created.</summary>
public enum CustomerType : byte
{
    /// <summary>A person.</summary>
    Individual = 1,

    /// <summary>A company or another registered business; it may carry a commercial registration.</summary>
    Business = 2,
}
