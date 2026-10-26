namespace WarehouseERP.Identity.Contracts;

/// <summary>Who a user is, for other modules (e.g. to show "created by").</summary>
public interface IUserDirectory
{
    /// <summary>Display names of the users among these ids that exist.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct);
}
