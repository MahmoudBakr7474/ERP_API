namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>Where documents get their issuing company from; the Organization module implements it.</summary>
public interface IDocumentIssuerSource
{
    /// <summary>The company, or null until it is set up (documents then print without company details).</summary>
    Task<DocumentIssuer?> GetAsync(CancellationToken ct);
}
