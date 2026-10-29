using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;

/// <summary>Write side of cashier shifts.</summary>
public interface IPosShiftRepository
{
    /// <summary>The shift's warehouse, for the scope check, or null when there is no such shift.</summary>
    Task<Guid?> FindWarehouseAsync(Guid shiftId, CancellationToken ct);

    /// <summary>
    /// The shift, tracked and locked (UPDLOCK) until the transaction ends, or null. With a row version, the save fails with
    /// a conflict unless it still has it. Its cash movements and events are not loaded; new ones are added.
    /// </summary>
    Task<PosShift?> GetForUpdateAsync(Guid shiftId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>The terminal's open shift, tracked and locked until the transaction ends, or null.</summary>
    Task<PosShift?> GetOpenForUpdateAsync(Guid terminalId, CancellationToken ct);

    Task<bool> HasOpenAsync(Guid terminalId, CancellationToken ct);

    void Add(PosShift shift);

    /// <summary>The save point; a second open shift of a terminal or a cashier comes back as a conflict.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
