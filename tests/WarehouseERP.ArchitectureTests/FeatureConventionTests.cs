using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using static WarehouseERP.ArchitectureTests.Solution;

namespace WarehouseERP.ArchitectureTests;

// The Application layer of every module is organised as Features/<BusinessArea>/<UseCase>/ (vertical slices
// grouped by business area): one folder and namespace per use case holding its Command or Query, Handler,
// Validator and its own DTOs. Use cases never reference each other; an area may hold a few types its use cases
// share (directly in Features/<Area>), and areas never reference each other.
public sealed class FeatureConventionTests
{
    private static string FeaturesNamespace(string module) => module + ".Application.Features";

    private static IEnumerable<Type> FeatureTypes(string module)
    {
        var root = FeaturesNamespace(module);
        return Assembly.Load(module + ".Application").GetTypes()
            .Where(t => t.Namespace == root || t.Namespace?.StartsWith(root + ".", StringComparison.Ordinal) == true);
    }

    // Segments after "<Module>.Application.Features": [Area] or [Area, UseCase].
    private static string[] Slice(string module, Type type) =>
        type.Namespace![FeaturesNamespace(module).Length..].Split('.', StringSplitOptions.RemoveEmptyEntries);

    private static bool IsUseCaseEntryPoint(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)
                                                          || i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)
                                                          || i.GetGenericTypeDefinition().FullName == "FluentValidation.IValidator`1"));

    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Feature_types_live_in_a_business_area_or_one_of_its_use_cases(string module)
    {
        var misplaced = FeatureTypes(module)
            .Where(t => Slice(module, t).Length is 0 or > 2)
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(misplaced);
    }

    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Each_handler_validator_and_request_sits_in_its_own_use_case_folder(string module)
    {
        var offenders = new List<string>();
        foreach (var type in FeatureTypes(module).Where(t => t.IsClass && !t.IsAbstract && IsUseCaseEntryPoint(t)))
        {
            var slice = Slice(module, type);
            if (slice.Length != 2)
            {
                offenders.Add($"{type.FullName}: not in Features/<Area>/<UseCase>");
                continue;
            }
            var useCase = slice[1];
            if (type.Name != useCase + "Handler" && type.Name != useCase + "Validator")
                offenders.Add($"{type.FullName}: expected {useCase}Handler or {useCase}Validator");

            // The request a handler takes is declared in the same use case folder.
            foreach (var handler in type.GetInterfaces().Where(i => i.IsGenericType
                         && (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) || i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))))
            {
                var request = handler.GetGenericArguments()[0];
                if (request.Namespace != type.Namespace || (request.Name != useCase + "Command" && request.Name != useCase + "Query"))
                    offenders.Add($"{type.FullName}: request {request.FullName} should be {type.Namespace}.{useCase}Command/Query");
            }
        }

        Assert.Empty(offenders);
    }

    // Read with Mono.Cecil rather than NetArchTest: async methods and lambdas compile into nested types whose
    // dependencies a namespace filter misses, and typeof() only shows up as an IL token. Every type, nested ones
    // included, is attributed to the namespace of its outermost declaring type.
    [Theory]
    [MemberData(nameof(ModuleData), MemberType = typeof(Solution))]
    public void Use_cases_and_areas_do_not_reference_each_other(string module)
    {
        var root = FeaturesNamespace(module);
        using var assembly = ModuleDefinition.ReadModule(Assembly.Load(module + ".Application").Location);
        var offenders = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var type in assembly.GetTypes())
        {
            var own = SliceOf(root, OwnerNamespace(type));
            if (own is not { Length: > 0 })
                continue;
            foreach (var referenced in References(type).SelectMany(Flatten))
            {
                var other = SliceOf(root, OwnerNamespace(referenced));
                if (other is not { Length: > 0 } || !Crosses(own, other))
                    continue;
                offenders.Add($"{type.FullName} -> {referenced.FullName}");
            }
        }

        Assert.Empty(offenders);
    }

    // A use case may use its own area's shared types; nothing may use another use case or another area, and an
    // area's shared types may not use its use cases.
    private static bool Crosses(string[] own, string[] other) =>
        own[0] != other[0] || (other.Length == 2 && (own.Length == 1 || own[1] != other[1]));

    private static string[]? SliceOf(string root, string ns) =>
        ns == root || ns.StartsWith(root + ".", StringComparison.Ordinal)
            ? ns[root.Length..].Split('.', StringSplitOptions.RemoveEmptyEntries)
            : null;

    private static string OwnerNamespace(TypeReference type)
    {
        while (type.DeclaringType is not null)
            type = type.DeclaringType;
        return type.Namespace;
    }

    private static IEnumerable<TypeReference> References(TypeDefinition type)
    {
        if (type.BaseType is not null)
            yield return type.BaseType;
        foreach (var contract in type.Interfaces)
            yield return contract.InterfaceType;
        foreach (var field in type.Fields)
            yield return field.FieldType;
        foreach (var property in type.Properties)
            yield return property.PropertyType;
        foreach (var method in type.Methods)
        {
            yield return method.ReturnType;
            foreach (var parameter in method.Parameters)
                yield return parameter.ParameterType;
            if (!method.HasBody)
                continue;
            foreach (var variable in method.Body.Variables)
                yield return variable.VariableType;
            foreach (var instruction in method.Body.Instructions)
            {
                switch (instruction.Operand)
                {
                    case TypeReference referenced:
                        yield return referenced;
                        break;
                    case MethodReference called:
                        yield return called.DeclaringType;
                        yield return called.ReturnType;
                        foreach (var parameter in called.Parameters)
                            yield return parameter.ParameterType;
                        if (called is GenericInstanceMethod generic)
                            foreach (var argument in generic.GenericArguments)
                                yield return argument;
                        break;
                    case FieldReference field:
                        yield return field.DeclaringType;
                        yield return field.FieldType;
                        break;
                }
            }
        }
    }

    // Generic arguments, arrays and by-ref types reduced to the named types they are made of.
    private static IEnumerable<TypeReference> Flatten(TypeReference type)
    {
        if (type is GenericParameter)
            yield break;
        yield return type.GetElementType();
        if (type is GenericInstanceType generic)
            foreach (var argument in generic.GenericArguments)
                foreach (var inner in Flatten(argument))
                    yield return inner;
    }
}
