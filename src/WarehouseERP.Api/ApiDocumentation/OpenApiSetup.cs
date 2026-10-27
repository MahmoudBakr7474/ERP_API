using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;

namespace WarehouseERP.Api.ApiDocumentation;

// The OpenAPI document is the contract the React client is generated from, so its schema names are public API. They
// are the CLR type names, except that a command bound straight from the request body is published as "...Request"
// (LoginCommand -> LoginRequest), the name the client sees for a body. So renaming any request or response type
// renames its schema: a contract change, which HostTests catches. Two types claiming one name fail the document
// instead of being merged silently. docs/api/openapi-v1.json is the committed copy, checked by HostTests.
public static class OpenApiSetup
{
    private const string CommandSuffix = "Command";
    private const string RequestSuffix = "Request";

    public static IServiceCollection AddErpOpenApi(this IServiceCollection services)
    {
        var claimed = new ConcurrentDictionary<string, Type>(StringComparer.Ordinal);
        services.AddOpenApi(o =>
        {
            o.CreateSchemaReferenceId = type =>
            {
                if (OpenApiOptions.CreateDefaultSchemaReferenceId(type) is not { } id)
                    return null; // inlined (primitives, collections)
                if (id.EndsWith(CommandSuffix, StringComparison.Ordinal))
                    id = id[..^CommandSuffix.Length] + RequestSuffix;
                var clrType = Nullable.GetUnderlyingType(type.Type) ?? type.Type; // T? shares T's schema
                var owner = claimed.GetOrAdd(id, clrType);
                return owner == clrType
                    ? id
                    : throw new InvalidOperationException($"OpenAPI schema name '{id}' is claimed by both {owner} and {clrType}.");
            };
            // A query bound with [AsParameters] documents its property names (Page, UserId); query keys are matched
            // case-insensitively, so the contract shows them in camelCase like every JSON property.
            o.AddOperationTransformer((operation, _, _) =>
            {
                foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
                    if (parameter.In == ParameterLocation.Query && parameter.Name is { } name)
                        parameter.Name = JsonNamingPolicy.CamelCase.ConvertName(name);
                return Task.CompletedTask;
            });
            // Every error body carries the ERP's extensions (ResultHttpMapping, ProblemDetailsSetup): the client branches
            // on errorCode and shows errors[] next to the fields, so the contract types them too.
            o.AddSchemaTransformer((schema, context, _) =>
            {
                if (context.JsonTypeInfo.Type == typeof(ProblemDetails))
                    AddErrorExtensions(schema);
                return Task.CompletedTask;
            });
        });
        return services;
    }

    private static void AddErrorExtensions(OpenApiSchema schema)
    {
        schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
        schema.Properties[ResultHttpMapping.ErrorCodeKey] = new OpenApiSchema { Type = JsonSchemaType.String };
        schema.Properties["traceId"] = new OpenApiSchema { Type = JsonSchemaType.String };
        schema.Properties["correlationId"] = new OpenApiSchema { Type = JsonSchemaType.String };
        schema.Properties[ResultHttpMapping.ErrorsKey] = new OpenApiSchema
        {
            Type = JsonSchemaType.Array,
            Items = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["property"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["code"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["message"] = new OpenApiSchema { Type = JsonSchemaType.String },
                },
                Required = new HashSet<string> { "property", "code", "message" },
            },
        };
        // traceId and correlationId come from the problem-details writer, which a client that doesn't accept JSON
        // bypasses; errorCode is in every problem body.
        schema.Required ??= new HashSet<string>();
        schema.Required.Add(ResultHttpMapping.ErrorCodeKey);
    }
}
