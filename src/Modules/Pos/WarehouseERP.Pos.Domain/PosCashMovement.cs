using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Domain;

/// <summary>Cash put into or taken out of a shift's drawer, with who, when and why. Never changes.</summary>
public sealed class PosCashMovement
{
    private PosCashMovement() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ShiftId { get; private set; }
    public PosCashMovementType MovementType { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = null!;
    public Guid? UserId { get; private set; }
    public DateTime AtUtc { get; private set; }

    internal static PosCashMovement Create(PosCashMovementType type, decimal amount, string reason, Guid? userId, DateTime atUtc) =>
        new() { MovementType = type, Amount = amount, Reason = reason, UserId = userId, AtUtc = atUtc };
}
