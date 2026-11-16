using LibraryHub.Data;
using LibraryHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Migration_Services;
using Serilog;
using ServiceDefaults;

// 2. Create console application builder

var builder = Host.CreateApplicationBuilder(args);

// Shared OpenTelemetry configuration
builder.AddServiceDefaults();

builder.Configuration.AddUserSecrets(
    typeof(MigrationService).Assembly,
    optional: false);

// 3. Read Aspire connection string
var connectionString =
    builder.Configuration.GetConnectionString("libraryhub")
    ?? throw new InvalidOperationException(
        "Connection string 'libraryhub' was not found.");

// 4. Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
            npgsqlOptions.MigrationsAssembly(
                typeof(ApplicationDbContext).Assembly));
});

// 5. Register Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 6. Build host and create scope
using var host = builder.Build();

await using var scope = host.Services.CreateAsyncScope();

var services = scope.ServiceProvider;

var logger = services.GetRequiredService<ILogger<Program>>();

try
{
    Log.Information("Migration Service started.");

    logger.LogInformation(
        "Starting database initialization.");

    // Apply pending migrations
    await MigrationService.MigrateAsync(services);

    // Seed roles and Admin user
    await SeedingService.SeedAsync(
        services,
        builder.Configuration);

    logger.LogInformation(
        "Database initialization completed successfully.");

    Log.Information(
        "Migration and seeding completed successfully.");
}
catch (Exception ex)
{
    logger.LogCritical(
        ex,
        "Database initialization failed.");

    Log.Fatal(
        ex,
        "Migration Service terminated due to an error.");

    throw;
}
finally
{
    Log.CloseAndFlush();
}