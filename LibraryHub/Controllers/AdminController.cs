using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


//   -- Admin Credentials --
//{
//    "email": "vickey@gmail.com",
//  "password": "13@Vickey0088"
//}

namespace LibraryHub.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private static readonly string[] value =
            [
                "Manage users",
                "Manager roles",
                "View system reports"
            ];

    [HttpGet("dashboard")]
    public IActionResult GetDashboard()
    {
        return Ok(new
        {
            message = "Welcome to Admin Dashboard",
            role = "Admin",
            features = value
        });
    }
}
