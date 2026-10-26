using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Application.Contracts.Sessions;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Sessions.ListSessions;

public sealed class ListSessionsHandler(ICurrentUser currentUser, ISessionRepository sessions)
    : IQueryHandler<ListSessionsQuery, IReadOnlyList<SessionResponse>>
{
    public async Task<Result<IReadOnlyList<SessionResponse>>> HandleAsync(ListSessionsQuery query, CancellationToken ct)
    {
        if (currentUser.UserId is not { } callerId)
            return UserErrors.NotFound;
        var userId = query.UserId ?? callerId;
        if (userId != callerId && !currentUser.HasPermission(IdentityPermissions.SessionsManage))
            return SessionErrors.AccessDenied;
        return Result.Success(await sessions.ListAsync(userId, ct));
    }
}
