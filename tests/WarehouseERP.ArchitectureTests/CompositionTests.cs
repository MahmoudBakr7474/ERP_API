using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Persistence;
using static WarehouseERP.ArchitectureTests.Solution;

namespace WarehouseERP.ArchitectureTests;

// The host is the composition root, the shared kernel stays small, nothing is circular, and the
// single ErpDbContext still respects module ownership.
public sealed class CompositionTests
{
    [Fact]
    public void Only_the_host_and_persistence_reference_module_outer_assemblies()
    {
        var offenders = All
            .Where(a => a.GetName().Name is not (ApiName or PersistenceName))
            .SelectMany(a => ProjectReferences(a).Where(r => Modules.Contains(r)).Select(r => $"{a.GetName().Name} -> {r}"))
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Nothing_references_the_host()
    {
        Assert.DoesNotContain(All, a => ProjectReferences(a).Contains(ApiName));
    }

    [Fact]
    public void Shared_kernel_references_no_project_and_no_framework_package()
    {
        var kernel = Assembly.Load(SharedKernelName);

        Assert.Empty(ProjectReferences(kernel));
        Assert.DoesNotContain(AllReferences(kernel), r => r.StartsWith("Microsoft.", StringComparison.Ordinal));
    }

    [Fact]
    public void Building_block_application_stays_free_of_ef_core_http_and_serilog()
    {
        var references = AllReferences(Assembly.Load(BuildingBlocksApplicationName)).ToList();

        Assert.DoesNotContain(references, r => r.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                                               || r.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                                               || r.StartsWith("Serilog", StringComparison.Ordinal));
        Assert.Equal([SharedKernelName], ProjectReferences(Assembly.Load(BuildingBlocksApplicationName)));
    }

    [Fact]
    public void Project_assemblies_form_no_cycle()
    {
        var graph = All.ToDictionary(a => a.GetName().Name!, a => ProjectReferences(a).ToList());
        var state = new Dictionary<string, bool>(); // false = on the current path, true = done

        void Visit(string node, Stack<string> path)
        {
            if (state.TryGetValue(node, out var done))
            {
                Assert.True(done, "Cycle: " + string.Join(" -> ", path.Reverse().Append(node)));
                return;
            }
            state[node] = false;
            path.Push(node);
            foreach (var next in graph.GetValueOrDefault(node, []))
                Visit(next, path);
            path.Pop();
            state[node] = true;
        }

        foreach (var node in graph.Keys)
            Visit(node, new Stack<string>());
    }

    [Fact]
    public void Every_entity_lives_in_its_modules_schema()
    {
        // Building the model needs no database connection.
        using var db = new ErpDbContext(new DbContextOptionsBuilder<ErpDbContext>().UseSqlServer("Server=unused").Options);

        var entities = db.Model.GetEntityTypes().Where(e => !e.IsOwned()).ToList();
        Assert.NotEmpty(entities);
        Assert.All(entities, entity => Assert.Equal(ExpectedSchema(entity.ClrType), entity.GetSchema()));
    }

    // WarehouseERP.<Module>.Domain -> <module>. Identity's tables also hold ASP.NET Core Identity's rows, its
    // Infrastructure user/role types and the Data Protection key ring.
    private static string ExpectedSchema(Type entity)
    {
        var assembly = entity.Assembly.GetName().Name!;
        if (assembly is IdentityName or AspNetIdentityStores or "Microsoft.AspNetCore.DataProtection.EntityFrameworkCore")
            return "identity";
        Assert.EndsWith(".Domain", assembly);
        return assembly.Split('.')[1].ToLowerInvariant();
    }
}
