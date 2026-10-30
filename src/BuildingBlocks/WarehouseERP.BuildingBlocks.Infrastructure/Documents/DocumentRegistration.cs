using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

public static class DocumentRegistration
{
    /// <summary>
    /// The shared PDF/XLSX/CSV services. The host calls it once; the Organization module supplies the issuing company
    /// (<see cref="IDocumentIssuerSource"/>).
    /// </summary>
    public static IServiceCollection AddErpDocuments(this IServiceCollection services)
    {
        services.AddOptions<DocumentOptions>()
            .BindConfiguration(DocumentOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(o => o.PdfLicense.HasValue,
                "Documents:PdfLicense must be Community, Professional or Enterprise: the QuestPDF license your organisation holds (docs/DOCUMENTS.md).")
            .Validate<IHostEnvironment>(
                (o, environment) => o.LogoPath is not { Length: > 0 } path
                                    || File.Exists(Path.Combine(environment.ContentRootPath, path)),
                "Documents:LogoPath must name an existing SVG file.")
            .ValidateOnStart();
        services.AddSingleton<DocumentAssets>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<DocumentFiles>();
        return services;
    }
}
