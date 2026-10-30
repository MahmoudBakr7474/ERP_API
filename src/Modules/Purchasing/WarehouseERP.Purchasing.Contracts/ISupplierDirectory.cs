namespace WarehouseERP.Purchasing.Contracts;

/// <summary>Read-only view of suppliers for other modules (Accounting names the supplier of each payable).</summary>
public interface ISupplierDirectory
{
    /// <summary>The suppliers among these ids, active or not; unknown ids are left out.</summary>
    Task<IReadOnlyList<SupplierInfo>> FindManyAsync(IReadOnlyCollection<Guid> supplierIds, CancellationToken ct);

    /// <summary>Up to <paramref name="take"/> active suppliers whose code, name or tax number contains the text, by code.</summary>
    Task<IReadOnlyList<SupplierInfo>> SearchActiveAsync(string text, int take, CancellationToken ct);
}
