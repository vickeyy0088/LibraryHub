using LibraryHub.DTOs.Pagination;
using LibraryHub.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryHub.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportsController(IReportRepository repo) : ControllerBase
{
    private readonly IReportRepository _repo = repo;

    [HttpGet("popular-books")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<IActionResult> PopularBook([FromQuery] PaginationParams query)
    {
        var result = await _repo.GetPopularBooksAsync(query);
        return Ok(result);
    }

    [HttpGet("member-activity")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<IActionResult> MemberActivity([FromQuery] PaginationParams query)
    {
        var result = await _repo.GetMemberActivityAsync(query);
        return Ok(result);
    }
}
