using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;
using static WarehouseERP.ArchitectureTests.Solution;

namespace WarehouseERP.ArchitectureTests;

// Clean Architecture inside each module, and modules only meeting through Contracts.
public sealed class ModuleBoundaryTests
{
    private static readonly string[] Frameworks =
        ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Microsoft.Data.SqlClient", "Serilog"];

    private static string[] Forbidden(IEnumerable<string> references, Func<string, bool> allowed) =>
        references.Where(r => !allowed(r)).ToArray();

    [Fact]
    public void Host_wires_the_known_modules()
    {
        Assert.Equal(["WarehouseERP.Catalog", "WarehouseERP.Identity", "WarehouseERP.Organization"], Modules);
    }

    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Module_has_domain_application_and_contracts_projects(string module)
    {
        foreach (var layer in new[] { ".Domain", ".Application", ".Contracts" })
            Assert.NotNull(Assembly.Load(module + layer));
    }

    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Domain_uses_only_the_shared_kernel_and_its_own_contracts(string module)
    {
        var domain = Assembly.Load(module + ".Domain");

        Assert.Empty(Forbidden(ProjectReferences(domain), r => r == SharedKernelName || r == module + ".Contracts"));
        Assert.Empty(Forbidden(AllReferences(domain), r => !r.StartsWith("Microsoft.", StringComparison.Ordinal)
                                                            && !r.StartsWith("FluentValidation", StringComparison.Ordinal)
                                                            && !r.StartsWith("Serilog", StringComparison.Ordinal)));
    }

    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Application_depends_inward_and_never_on_ef_core_http_or_serilog(string module)
    {
        var application = Assembly.Load(module + ".Application");

        Assert.Empty(Forbidden(ProjectReferences(application), r =>
            r == module + ".Domain" || IsContracts(r) || r == BuildingBlocksApplicationName || r == SharedKernelName));
        Assert.Empty(Forbidden(AllReferences(application), r => !Frameworks.Any(f => r.StartsWith(f, StringComparison.Ordinal))));
    }

    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Infrastructure_and_presentation_reach_no_other_module_nor_the_composition_root(string module)
    {
        var outer = Assembly.Load(module);

        Assert.Empty(Forbidden(ProjectReferences(outer), r =>
            r.StartsWith(module + ".", StringComparison.Ordinal) && !IsContracts(r) // own Domain/Application
            || IsContracts(r)
            || r is SharedKernelName or BuildingBlocksApplicationName or BuildingBlocksInfrastructureName));
    }

    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Presentation_goes_through_the_application_and_never_touches_ef_core(string module)
    {
        var result = Types.InAssembly(Assembly.Load(module))
            .That().ResideInNamespace(module + ".Presentation")
            .ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore", module + ".Infrastructure", module + ".Domain")
            .GetResult();

        Assert.True(result.IsSuccessful, "Offending types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Infrastructure_configures_only_its_own_modules_entities(string module)
    {
        var configured = Assembly.Load(module).GetTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
            .Select(i => i.GetGenericArguments()[0])
            .ToList();

        Assert.NotEmpty(configured);
        Assert.All(configured, entity => Assert.Contains(entity.Assembly.GetName().Name, OwnEntityAssemblies(module)));
    }

    // A module's entities are its Domain's. Identity also maps ASP.NET Core Identity's own row types and its
    // Infrastructure user/role types (ADR 0009); no other module may.
    private static string[] OwnEntityAssemblies(string module) => module == IdentityName
        ? [module + ".Domain", module, AspNetIdentityStores]
        : [module + ".Domain"];

    [Fact]
    public void Only_identity_and_persistence_reference_aspnet_core_identity()
    {
        var offenders = All
            .Where(a => a.GetName().Name is not (IdentityName or PersistenceName))
            .SelectMany(a => AllReferences(a)
                .Where(r => r.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.Ordinal)
                            || r.StartsWith("Microsoft.Extensions.Identity", StringComparison.Ordinal))
                .Select(r => $"{a.GetName().Name} -> {r}"))
            .ToList();

        Assert.Empty(offenders);
    }

    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Contracts_are_plain_types_with_no_references(string module)
    {
        var contracts = Assembly.Load(module + ".Contracts");

        Assert.Empty(ProjectReferences(contracts));
        Assert.Empty(Forbidden(AllReferences(contracts), r => r.StartsWith("System", StringComparison.Ordinal) || r == "netstandard"));
    }
}
