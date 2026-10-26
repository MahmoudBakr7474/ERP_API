using System.Reflection;
using System.Text.RegularExpressions;
using WarehouseERP.SharedKernel.Results;
using static WarehouseERP.ArchitectureTests.Solution;

namespace WarehouseERP.ArchitectureTests;

// Where each kind of type lives inside a module (ADR 0011). Only placements that carry meaning are checked;
// implementation details inside a use case or an Infrastructure folder are left free. Reflection checks the compiled assemblies; the file
// rule reads the sources, which is reliable because file-scoped namespaces are enforced (.editorconfig), so a
// top-level type always starts at column 0 and a nested one never does.
public sealed partial class ApplicationLayoutTests
{
    private static IEnumerable<Type> TypesOf(string assembly) =>
        Assembly.Load(assembly).GetTypes().Where(t => !t.Name.StartsWith('<') && t.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() is null);

    private static bool DeclaresErrors(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly).Any(f => f.FieldType == typeof(Error))
        || type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly).Any(m => m.ReturnType == typeof(Error) && !m.IsSpecialName);

    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Application_errors_are_grouped_by_business_area_in_Errors(string module)
    {
        var offenders = TypesOf(module + ".Application")
            .Where(DeclaresErrors)
            .Where(t => t.Namespace != module + ".Application.Errors" || !t.Name.EndsWith("Errors", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(offenders);
    }

    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Domain_errors_are_named_after_their_entity(string module)
    {
        var offenders = TypesOf(module + ".Domain")
            .Where(DeclaresErrors)
            .Where(t => !t.Name.EndsWith("DomainErrors", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(offenders);
    }

    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Application_interfaces_live_in_Abstractions_Interfaces_grouped_by_responsibility(string module)
    {
        var root = module + ".Application.Abstractions.Interfaces.";
        var offenders = TypesOf(module + ".Application")
            .Where(t => t.IsInterface)
            .Where(t => t.Namespace?.StartsWith(root, StringComparison.Ordinal) != true || t.Namespace[root.Length..].Contains('.', StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(offenders);
    }

    // Reused application contracts sit in Contracts/<Area>/ (no DTOs or Requests/Responses levels); a contract only
    // one use case needs stays in that use case's folder, which FeatureConventionTests already constrains.
    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Reused_application_contracts_are_grouped_by_business_area(string module)
    {
        var root = module + ".Application.Contracts";
        var offenders = TypesOf(module + ".Application")
            .Where(t => t.Namespace == root || t.Namespace?.StartsWith(root + ".", StringComparison.Ordinal) == true)
            .Where(t => t.Namespace == root || t.Namespace![(root.Length + 1)..].Contains('.', StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(offenders);
    }

    // HTTP-only request records (a route id, a server-side field or a lenient optional list sets them apart from the
    // command) live in Presentation/Contracts/<Area>/. A request identical to its command is not written at all: the
    // endpoint binds the command.
    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Http_request_records_live_in_Presentation_Contracts_by_area(string module)
    {
        var offenders = TypesOf(module)
            .Where(t => t.Name.EndsWith("Request", StringComparison.Ordinal) || t.Name.EndsWith("Response", StringComparison.Ordinal))
            .Where(t => !HttpContractNamespace().IsMatch((t.Namespace ?? "").StartsWith(module, StringComparison.Ordinal) ? t.Namespace![module.Length..] : ""))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(offenders);
    }

    [GeneratedRegex(@"^\.Presentation\.Contracts\.[A-Z]\w*$")]
    private static partial Regex HttpContractNamespace();

    [GeneratedRegex(@"^(?:public|internal|file)\b(?:\s+(?:sealed|static|abstract|partial|readonly))*\s+(?:class|record(?:\s+(?:class|struct))?|interface|enum|struct)\s+([A-Za-z_]\w*)", RegexOptions.Multiline)]
    private static partial Regex TopLevelType();

    // A file that genuinely needs more than one top-level type is listed here with its reason (ADR 0011).
    private static readonly Dictionary<string, string> MultiTypeFileExceptions = new(StringComparer.Ordinal);

    [Fact]
    public void Every_module_source_file_declares_one_top_level_type_named_like_the_file()
    {
        var modules = Path.Combine(RepositoryRoot(), "src", "Modules");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(modules, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            var types = TopLevelType().Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value).ToList();
            var expected = Path.GetFileNameWithoutExtension(file);
            if ((types.Count != 1 || types[0] != expected) && !MultiTypeFileExceptions.ContainsKey(Path.GetRelativePath(modules, file).Replace('\\', '/')))
                offenders.Add($"{Path.GetRelativePath(modules, file)}: [{string.Join(", ", types)}]");
        }

        Assert.NotEmpty(Directory.EnumerateFiles(modules, "*.cs", SearchOption.AllDirectories));
        Assert.Empty(offenders);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "WarehouseERP.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("WarehouseERP.slnx not found above the test output.");
    }
}
