using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;

namespace WarehouseERP.Persistence;

/// <summary>Bound to the <c>Database</c> configuration section. The connection string never lives in source control.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string ConnectionString { get; init; } = "";

    [Range(5, 600)]
    public int CommandTimeoutSeconds { get; init; } = 30;
}

public static class PersistenceRegistration
{
    public static IServiceCollection AddErpPersistence(this IServiceCollection services)
    {
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AuditingSaveChangesInterceptor>();
        services.AddDbContext<ErpDbContext>((sp, options) =>
        {
            var database = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(database.ConnectionString, sql => sql.CommandTimeout(database.CommandTimeoutSeconds));
            options.AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>());
            // EF Core logs every failed command, query and save as an error. The application owns that
            // decision: an expected failure (a taken SKU) becomes a Result at the save point and an
            // unexpected one is logged once by the host's exception handler. EF's copies go to Debug.
            options.ConfigureWarnings(w => w.Log(
                (RelationalEventId.CommandError, LogLevel.Debug),
                (RelationalEventId.ConnectionError, LogLevel.Debug),
                (CoreEventId.QueryIterationFailed, LogLevel.Debug),
                (CoreEventId.SaveChangesFailed, LogLevel.Debug),
                (CoreEventId.OptimisticConcurrencyException, LogLevel.Debug)));
        });
        // Modules depend on DbContext, not ErpDbContext, so they never reference this project.
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<ErpDbContext>());

        // Keys in the database, so a restart or a second instance still accepts issued anti-forgery tokens.
        services.AddDataProtection()
            .SetApplicationName("WarehouseERP")
            .PersistKeysToDbContext<ErpDbContext>();
        return services;
    }
}
