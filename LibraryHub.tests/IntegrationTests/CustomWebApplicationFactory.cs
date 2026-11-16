using LibraryHub.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // This class is used to create a custom WebApplicationFactory for integration testing.
    private readonly string _dbName = $"LibraryHub_IntegrationTestDb_{Guid.NewGuid()}";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Injecting Dummy Configuration for Integration Tests
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:libraryhub"] = "Host=localhost;Database=FakeTestDb",
                ["Jwt:Key"] = "SuperSecretIntegrationTestingKey1234567890!",
                ["Jwt:Issuer"] = "LibraryHubTestIssuer",
                ["Jwt:Audience"] = "LibraryHubTestAudience"
            });
        });

        // ConfigureTestServices replaces PostgreSQL with EF InMemory and JWT with TestAuthHandler

        builder.ConfigureTestServices(Services =>
        {
            // remove the existing DbContext registration and replace it with an in-memory database for testing
            Services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            Services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            // Register the in-memory database for testing
            Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            // Register the TestAuthHandler for integration testing
            Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>( // AddScheme is used to register a custom authentication handler for integration testing.
                TestAuthHandler.SchemeName, _ => { });

        });
    }
}
