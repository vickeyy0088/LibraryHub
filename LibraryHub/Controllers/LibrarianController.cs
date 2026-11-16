using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Librarian Credentials
/**    "fullName": "Ashish",
       "email": "ashish@gmail.com",
**/

namespace LibraryHub.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Librarian")]
public class LibrarianController : ControllerBase
{
    private static readonly string[] value =
            [
            "Manage books",
            "Manage authors",
            "Manage categories",
            "Manage book loans"
        ];

    [HttpGet("dashboard")]
    public IActionResult GetDashboard()
    {
        return Ok(new
        {
            message = "Welcome to the Librarian Dashboard.",
            role = "Librarian",
            features = value
        });
    }
}
