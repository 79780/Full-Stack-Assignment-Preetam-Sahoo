using IpiPro.Api.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IpiPro.Api.Data;

/// <summary>
/// Lets `dotnet ef migrations add` construct the context outside a request, where there is
/// no tenant. Migrations only need the model shape, never a tenant-scoped query.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=ipipro.db")
            .Options;

        return new AppDbContext(options, new TenantContext());
    }
}
