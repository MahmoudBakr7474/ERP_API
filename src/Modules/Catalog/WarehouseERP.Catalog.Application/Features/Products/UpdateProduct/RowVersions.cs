namespace WarehouseERP.Catalog.Application.Features.Products.UpdateProduct;

internal static class RowVersions
{
    // SQL Server rowversion: 8 bytes, sent to clients as base64.
    public static bool TryParse(string? value, out byte[] rowVersion)
    {
        rowVersion = [];
        if (string.IsNullOrEmpty(value)) return false;
        var buffer = new byte[8];
        if (!Convert.TryFromBase64String(value, buffer, out var written) || written != 8) return false;
        rowVersion = buffer;
        return true;
    }
}
