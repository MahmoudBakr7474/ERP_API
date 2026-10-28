using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>A person at the supplier. Replaced with the supplier's other details; its history is a temporal table.</summary>
public sealed class SupplierContact
{
    private SupplierContact() { } // EF Core

    public Guid Id { get; private set; }
    public Guid SupplierId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? JobTitle { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public bool IsPrimary { get; private set; }

    internal static SupplierContact Create(SupplierContactInput input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Name);
        return new SupplierContact
        {
            Name = input.Name.Trim(), JobTitle = Text.Optional(input.JobTitle), Phone = Text.Optional(input.Phone),
            Email = Text.Optional(input.Email), IsPrimary = input.IsPrimary,
        };
    }
}
