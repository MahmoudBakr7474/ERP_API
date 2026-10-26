using System.Reflection;

namespace WarehouseERP.ArchitectureTests;

// The project assemblies as compiled, discovered from the host so a new module is checked without
// editing these tests. A module is an assembly WarehouseERP.<Name> the host references, with
// WarehouseERP.<Name>.Domain, .Application and .Contracts beside it.
internal static class Solution
{
    public const string Prefix = "WarehouseERP.";
    public const string ApiName = "WarehouseERP.Api";
    public const string PersistenceName = "WarehouseERP.Persistence";
    public const string SharedKernelName = "WarehouseERP.SharedKernel";
    public const string BuildingBlocksApplicationName = "WarehouseERP.BuildingBlocks.Application";
    public const string BuildingBlocksInfrastructureName = "WarehouseERP.BuildingBlocks.Infrastructure";

    public static readonly Assembly Host = Assembly.Load(ApiName);

    public static IEnumerable<string> ProjectReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith(Prefix, StringComparison.Ordinal));

    public static IEnumerable<string> AllReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!);

    /// <summary>Module names, e.g. "WarehouseERP.Catalog".</summary>
    public static IReadOnlyList<string> Modules { get; } = ProjectReferences(Host)
        .Where(n => n.Count(c => c == '.') == 1 && n is not PersistenceName and not SharedKernelName)
        .Order()
        .ToList();

    public static bool IsContracts(string name) => name.EndsWith(".Contracts", StringComparison.Ordinal);

    /// <summary>Every project assembly reachable from the host, the host included.</summary>
    public static IReadOnlyList<Assembly> All { get; } = Closure();

    private static List<Assembly> Closure()
    {
        var seen = new Dictionary<string, Assembly>(StringComparer.Ordinal) { [ApiName] = Host };
        var queue = new Queue<Assembly>([Host]);
        while (queue.TryDequeue(out var assembly))
            foreach (var name in ProjectReferences(assembly).Where(n => !seen.ContainsKey(n)))
                queue.Enqueue(seen[name] = Assembly.Load(name));
        return [.. seen.Values];
    }

    public static TheoryData<string> ModuleData() => new(Modules);
}
