namespace WarehouseERP.Sales.Contracts;

/// <summary>Read-only view of customers for other modules (POS names a registered customer on a receipt).</summary>
public interface ICustomerDirectory
{
    /// <summary>The customer, active or not, or null when no customer has this id.</summary>
    Task<SalesCustomerInfo?> FindAsync(Guid customerId, CancellationToken ct);

    /// <summary>
    /// Up to <paramref name="take"/> active customers whose code, name, tax number or phone contains the text, by code.
    /// </summary>
    Task<IReadOnlyList<SalesCustomerInfo>> SearchActiveAsync(string text, int take, CancellationToken ct);
}
