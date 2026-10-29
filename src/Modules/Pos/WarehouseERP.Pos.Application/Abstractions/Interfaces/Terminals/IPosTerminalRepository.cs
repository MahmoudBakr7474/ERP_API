using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Terminals;

/// <summary>Write side of terminals; implemented over the shared ErpDbContext.</summary>
public interface IPosTerminalRepository
{
    /// <summary>The terminal, untracked, or null.</summary>
    Task<PosTerminal?> FindAsync(Guid terminalId, CancellationToken ct);

    Task<bool> CodeExistsAsync(string code, CancellationToken ct);

    /// <summary>Tracked, or null; with a row version, the save fails with a conflict unless it still has it.</summary>
    Task<PosTerminal?> GetForUpdateAsync(Guid terminalId, byte[]? expectedRowVersion, CancellationToken ct);

    void Add(PosTerminal terminal);

    /// <summary>The save point; a code taken at the same moment comes back as a conflict.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
