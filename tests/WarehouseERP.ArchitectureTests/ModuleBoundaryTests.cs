using System.Reflection;

namespace WarehouseERP.ArchitectureTests;

// Module boundaries, read from the compiled assemblies. A module may use the shared kernel and
// any module's Contracts, never another module's internals, the persistence project or the host.
// Modules are discovered from the API host's references, so a new module is checked automatically.
public sealed class ModuleBoundaryTests
{
    private const string Prefix = "WarehouseERP.";
    private const string SharedKernel = "WarehouseERP.SharedKernel";
    private static readonly string[] Composition = ["WarehouseERP.Api", "WarehouseERP.Persistence"];

    private static readonly Assembly Host = Assembly.Load("WarehouseERP.Api");

    private static IEnumerable<string> ProjectReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith(Prefix));

    private static bool IsContracts(string name) => name.EndsWith(".Contracts");

    private static IEnumerable<string> ModuleNames() =>
        ProjectReferences(Host).Where(n => n != SharedKernel && !Composition.Contains(n) && !IsContracts(n));

    public static TheoryData<string> Modules() => new(ModuleNames());

    [Fact]
    public void Host_wires_the_known_modules()
    {
        Assert.Equal(["WarehouseERP.Catalog", "WarehouseERP.Organization"], ModuleNames().Order());
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Module_uses_only_the_shared_kernel_and_contracts(string module)
    {
        var forbidden = ProjectReferences(Assembly.Load(module))
            .Where(n => n != SharedKernel && !IsContracts(n));
        Assert.Empty(forbidden);
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Contracts_reference_no_project(string module)
    {
        Assert.Empty(ProjectReferences(Assembly.Load(module + ".Contracts")));
    }

    [Fact]
    public void Shared_kernel_references_no_project()
    {
        Assert.Empty(ProjectReferences(Assembly.Load(SharedKernel)));
    }
}
