using WarehouseERP.Identity.Application.Contracts.Authentication;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Application.Features.Authentication;

// A freshly issued access/refresh pair: the tokens to hand out once, and the hashes the session keeps.
internal sealed record IssuedPair(string AccessToken, string RefreshToken, IssuedApiTokens Hashes)
{
    public ApiTokensResponse ToResponse(Guid sessionId) =>
        new(sessionId, AccessToken, Hashes.AccessExpiresAtUtc, RefreshToken, Hashes.RefreshExpiresAtUtc);
}
