using LibraryHub.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Migration_Services
{
    public class MigrationService
    {
        public static async Task MigrateAsync(IServiceProvider service)
        {
            var dbcontext = service.GetRequiredService<ApplicationDbContext>(); //It resolves the already registered ApplicationDbContext
                                                                                //through dependency injection.

            var logger = service.GetRequiredService<ILogger<MigrationService>>();

            logger.LogInformation("Applying database migrations.");

            await dbcontext.Database.MigrateAsync(); // applies all pending migrations to PostgreSQL

            logger.LogInformation("Database migrations completed successfully.");
        }
    }
}
