using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Sales.Domain;

/// <summary>A person to talk to at the customer. Replaced with the customer's other details; its history is a temporal table.</summary>
public sealed class CustomerContact
{
    private CustomerContact() { } // EF Core

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? JobTitle { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public bool IsPrimary { get; private set; }

    internal static CustomerContact Create(CustomerContactInput input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Name);
        return new CustomerContact
        {
            Name = input.Name.Trim(), JobTitle = Text.Optional(input.JobTitle), Phone = Text.Optional(input.Phone),
            Email = Text.Optional(input.Email), IsPrimary = input.IsPrimary,
        };
    }
}
