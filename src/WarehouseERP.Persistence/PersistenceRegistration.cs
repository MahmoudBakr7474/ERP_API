using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WarehouseERP.Persistence;

public static class PersistenceRegistration
{
    public static IServiceCollection AddErpPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ErpDbContext>(o => o.UseSqlServer(connectionString));
        // Modules depend on DbContext, not ErpDbContext, so they never reference this project.
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<ErpDbContext>());
        return services;
    }
}
