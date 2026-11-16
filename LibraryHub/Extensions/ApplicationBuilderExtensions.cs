//using LibraryHub.Data;
//using LibraryHub.Models;
//using Microsoft.AspNetCore.Identity;

//namespace LibraryHub.Extensions;

//public static class ApplicationBuilderExtensions
//{
//    public static async Task SeedAdminAsync(this WebApplication app)
//    {
//        using var scope = app.Services.CreateScope();

//        var userManager = scope.ServiceProvider
//            .GetRequiredService<UserManager<ApplicationUser>>();

//        var roleManager = scope.ServiceProvider
//            .GetRequiredService<RoleManager<IdentityRole>>();

//        await IdentitySeeder.SeedRolesAsync(roleManager);

//        const string adminEmail = "vickey@gmail.com";
//        const string adminRole = "Admin";

//        var adminPassword = app.Configuration["AdminSeed:Password"]
//            ?? throw new InvalidOperationException(
//                "Admin seed password is not configured.");

//        //if (!await roleManager.RoleExistsAsync(adminRole))
//        //{
//        //    var roleResult = await roleManager.CreateAsync(
//        //        new IdentityRole(adminRole));

//        //    if (!roleResult.Succeeded)
//        //    {
//        //        throw new InvalidOperationException(
//        //            string.Join("; ", roleResult.Errors.Select(x => x.Description)));
//        //    }
//        //}

//        // Seed all three roles.
//        await IdentitySeeder.SeedRolesAsync(roleManager);

//        // find existing admin by email
//        var user = await userManager.FindByEmailAsync(adminEmail);

//        if (user is null)
//        {
//            user = new ApplicationUser
//            {
//                UserName = adminEmail,
//                Email = adminEmail,
//                EmailConfirmed = true,
//                FullName = "LibraryHub Administrator"
//            };

//            var createResult = await userManager.CreateAsync(
//                user,
//                adminPassword);

//            if (!createResult.Succeeded)
//            {
//                throw new InvalidOperationException(
//                    string.Join(
//                        "; ",
//                        createResult.Errors.Select(x => x.Description)));
//            }
//        }

//        if (!await userManager.IsInRoleAsync(user, adminRole))
//        {
//            var roleResult = await userManager.AddToRoleAsync(
//                user,
//                adminRole);

//            if (!roleResult.Succeeded)
//            {
//                throw new InvalidOperationException(
//                    string.Join(
//                        "; ",
//                        roleResult.Errors.Select(x => x.Description)));
//            }
//        }
//    }
//}


