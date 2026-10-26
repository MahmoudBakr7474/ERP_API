using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WarehouseERP.Persistence.Design;

public sealed class ErpDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ErpDbContext>
{
    public ErpDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ERP_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Set ERP_SQL_CONNECTION to a SQL Server connection string before running dotnet ef.");
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(ErpDbContext).Assembly.FullName))
            .Options;
        return new ErpDbContext(options);
    }
}
