//using Microsoft.AspNetCore.Identity;

//namespace LibraryHub.Data;

//public class IdentitySeeder
//{
//    //  fixed, read-only array containing the three roles
//    private static readonly string[] Roles =
//    {
//        "Admin",
//        "Librarian",
//        "Member"
//    };

//    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
//    {
//        foreach (var role in Roles)
//        {
//            //if (!await roleManager.RoleExistsAsync(role))
//            //{
//            //    await roleManager.CreateAsync(new IdentityRole(role));
//            //}

//            if (await roleManager.RoleExistsAsync(role))
//                continue;

//            var result = await roleManager.CreateAsync(
//                new IdentityRole(role));

//            if (!result.Succeeded)
//            {
//                throw new InvalidOperationException(
//                    $"Failed to create role '{role}': " +
//                    string.Join("; ",
//                        result.Errors.Select(x => x.Description)));
//            }
//        }
//    }
//}
