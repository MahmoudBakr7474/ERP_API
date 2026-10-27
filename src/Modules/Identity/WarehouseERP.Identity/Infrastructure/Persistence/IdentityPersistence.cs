using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

internal sealed class IdentityPersistence(DbContext db) : IIdentityPersistence
{
    // Unique indexes whose violation is an expected outcome: ASP.NET Core Identity's UserNameIndex and RoleNameIndex
    // (two requests creating the same name at once), and one UserAccess per user (two first-time scope edits).
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["UserNameIndex"] = UserErrors.UserNameTaken,
        ["RoleNameIndex"] = RoleErrors.NameTaken,
        ["IX_UserAccess_UserId"] = CommonErrors.ConcurrencyConflict,
        ["PK_UserRoles"] = CommonErrors.ConcurrencyConflict,
        ["PK_RetiredRefreshTokens"] = CommonErrors.ConcurrencyConflict,
    };

    public Task<Result> SaveChangesAsync(CancellationToken ct) => DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);

    public void DiscardChanges() => db.ChangeTracker.Clear();
}
