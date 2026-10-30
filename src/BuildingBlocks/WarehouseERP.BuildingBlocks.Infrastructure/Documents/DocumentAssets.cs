using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// Process-wide document resources: the QuestPDF license, the embedded PDF font and the logo. Singleton; the font is
/// registered once per process (QuestPDF's font manager is global).
/// </summary>
public sealed class DocumentAssets
{
    /// <summary>IBM Plex Sans Arabic (SIL OFL 1.1): Arabic and Latin in one family, the same as the web app.</summary>
    public const string FontFamily = "IBM Plex Sans Arabic";

    /// <summary>QuestPDF's bundled Latin font, used only for characters the main font lacks.</summary>
    public const string FallbackFontFamily = "Lato";

    private const string ResourcePrefix = "WarehouseERP.Documents.";
    private static readonly Lock FontLock = new();
    private static bool _fontsRegistered;

    public DocumentAssets(IOptions<DocumentOptions> options, IHostEnvironment environment)
    {
        var settings = options.Value;
        QuestPDF.Settings.License = settings.PdfLicense switch
        {
            PdfLicense.Community => QuestPDF.Infrastructure.LicenseType.Community,
            PdfLicense.Professional => QuestPDF.Infrastructure.LicenseType.Professional,
            PdfLicense.Enterprise => QuestPDF.Infrastructure.LicenseType.Enterprise,
            _ => throw new InvalidOperationException("Documents:PdfLicense is not configured."),
        };
        // Same output on every machine: only the fonts shipped with the application. A product name in a script the
        // fonts lack prints replacement glyphs instead of failing the whole document.
        QuestPDF.Settings.UseSystemFonts = false;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
        RegisterFonts();

        LogoSvg = settings.LogoPath is { Length: > 0 } path
            ? File.ReadAllText(Path.Combine(environment.ContentRootPath, path))
            : ReadResourceText("codlek-logo.svg");
    }

    /// <summary>The logo printed on documents: the configured one, or the approved Codlek logo.</summary>
    public string LogoSvg { get; }

    private static void RegisterFonts()
    {
        lock (FontLock)
        {
            if (_fontsRegistered)
                return;
            foreach (var weight in new[] { "Regular", "SemiBold", "Bold" })
            {
                using var font = OpenResource($"IBMPlexSansArabic-{weight}.ttf");
                FontManager.RegisterFontFromStream(font);
            }
            _fontsRegistered = true;
        }
    }

    private static string ReadResourceText(string name)
    {
        using var reader = new StreamReader(OpenResource(name));
        return reader.ReadToEnd();
    }

    private static Stream OpenResource(string name) =>
        typeof(DocumentAssets).Assembly.GetManifestResourceStream(ResourcePrefix + name)
        ?? throw new InvalidOperationException($"Embedded document resource {name} is missing.");
}
