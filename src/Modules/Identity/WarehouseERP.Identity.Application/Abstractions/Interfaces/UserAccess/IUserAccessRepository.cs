namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.UserAccess;

// "Domain.UserAccess": inside this namespace the simple name UserAccess is the namespace itself.

/// <summary>Where each user may act.</summary>
public interface IUserAccessRepository
{
    Task<Domain.UserAccess?> GetAsync(Guid userId, CancellationToken ct);

    void Add(Domain.UserAccess access);
}
