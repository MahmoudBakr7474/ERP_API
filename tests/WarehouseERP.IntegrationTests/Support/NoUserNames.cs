using WarehouseERP.Identity.Contracts;

namespace WarehouseERP.IntegrationTests;

/// <summary>Stands in for Identity's user directory where only the modules are wired.</summary>
internal sealed class NoUserNames : IUserDirectory
{
    public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
}
