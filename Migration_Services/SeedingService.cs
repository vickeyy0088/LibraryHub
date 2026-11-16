using LibraryHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Migration_Services;

public class SeedingService
{
    private static readonly string[] Roles =
    {
        "Admin",
        "Librarian",
        "Member"
    };

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {

        var logger = services.GetRequiredService<ILogger<SeedingService>>();

        var userManager = services
            .GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager = services
            .GetRequiredService<RoleManager<IdentityRole>>();

        // Admin configuration
        const string adminEmail = "vickey@gmail.com";
        const string adminRole = "Admin";

        var adminPassword = configuration["AdminSeed:Password"]
            ?? throw new InvalidOperationException(
                "Admin seed password is not configured.");

        // 1. Create roles if they do not exist
        foreach (var role in Roles)
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var result = await roleManager.CreateAsync(
                new IdentityRole(role));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role '{role}': " +
                    string.Join("; ",
                        result.Errors.Select(x => x.Description)));
            }
        }

        // 2. Find existing Admin user
        var user = await userManager.FindByEmailAsync(adminEmail);

        // 3. Create Admin user if it does not exist
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "LibraryHub Administrator"
            };

            var createResult = await userManager.CreateAsync(
                user,
                adminPassword);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to create Admin user: " +
                    string.Join("; ",
                        createResult.Errors.Select(x => x.Description)));
            }
        }

        // 4. Assign Admin role if not already assigned
        if (!await userManager.IsInRoleAsync(user, adminRole))
        {
            var roleResult = await userManager.AddToRoleAsync(
                user,
                adminRole);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to assign Admin role: " +
                    string.Join("; ",
                        roleResult.Errors.Select(x => x.Description)));
            }
        }

        logger.LogInformation("Database seeding completed successfully.");
    }
}