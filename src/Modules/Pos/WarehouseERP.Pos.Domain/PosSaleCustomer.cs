namespace WarehouseERP.Pos.Domain;

/// <summary>Who bought, as printed on the receipt: a registered customer's snapshot, or a walk-in's optional name and phone.</summary>
public sealed record PosSaleCustomer(Guid? CustomerId, string? Name, string? Phone, string? TaxNumber)
{
    public static PosSaleCustomer WalkIn(string? name, string? phone) => new(null, name, phone, null);
}
