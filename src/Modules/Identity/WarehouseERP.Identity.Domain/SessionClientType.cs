namespace WarehouseERP.Identity.Domain;

public enum SessionClientType : byte
{
    /// <summary>Browser (React): the token travels in an HttpOnly cookie.</summary>
    Web = 1,

    /// <summary>POS or API tool: a short bearer access token plus a rotating refresh token.</summary>
    Api = 2,
}
