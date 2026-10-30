using Microsoft.AspNetCore.Http;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// A generated file sent as an attachment. Business files are never cached by browsers or proxies, and the declared
/// type is the only one a browser may use.
/// </summary>
public sealed class FileDownload(byte[] content, string contentType, string fileName) : IResult
{
    public string FileName => fileName;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.XContentTypeOptions = "nosniff";
        // Content-Disposition carries both filename= and an RFC 5987 filename*=.
        return TypedResults.File(content, contentType, fileName).ExecuteAsync(httpContext);
    }
}
