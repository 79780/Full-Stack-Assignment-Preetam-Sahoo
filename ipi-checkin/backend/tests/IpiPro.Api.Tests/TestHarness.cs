using IpiPro.Api.Data;
using IpiPro.Api.Services;
using IpiPro.Api.Tenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IpiPro.Api.Tests;

/// <summary>
/// A real SQLite database held in memory — not the EF in-memory provider, which ignores
/// unique indexes and FK behaviour, i.e. precisely the parts we want to trust. The
/// connection stays open for the harness lifetime because closing it drops the database.
///
/// Migrate() rather than EnsureCreated(), so the committed migration is exercised on every
/// test run: if it ever drifts from the model, the suite says so.
/// </summary>
public sealed class TestHarness : IAsyncDisposable
{
    public const int Northgate = 1;
    public const int Ridgeview = 2;

    private readonly SqliteConnection _connection;

    private TestHarness(SqliteConnection connection) => _connection = connection;

    public static async Task<TestHarness> CreateAsync()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var harness = new TestHarness(connection);

        await using var db = harness.NewContext(new TenantContext());
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);

        return harness;
    }

    /// <summary>A context and service scoped to one lab, exactly as a request would be.</summary>
    public (AppDbContext Db, CheckInService Service) AsLab(int labId)
    {
        var tenant = new TenantContext();
        tenant.Resolve(labId);

        var db = NewContext(tenant);
        return (db, new CheckInService(db, TimeProvider.System));
    }

    /// <summary>A context with no tenant, for asserting on raw rows past the filters.</summary>
    public AppDbContext AsSystem() => NewContext(new TenantContext());

    private AppDbContext NewContext(ITenantContext tenant)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new AppDbContext(options, tenant);
    }

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}
