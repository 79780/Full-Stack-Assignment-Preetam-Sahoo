using IpiPro.Api.Data;
using IpiPro.Api.Infrastructure;
using IpiPro.Api.Services;
using IpiPro.Api.Tenancy;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=ipipro.db";

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));

// One TenantContext instance per request, exposed only through its interface everywhere
// except the middleware that populates it.
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CheckInService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Migrate + seed on boot so `dotnet run` is the only setup step a reviewer needs.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Order matters: the handler must wrap everything it is meant to catch, including the
// tenant middleware, and CORS headers must survive an error response.
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseMiddleware<TenantMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();

/// <summary>Exposed so the test project can spin the app up with WebApplicationFactory.</summary>
public partial class Program;
