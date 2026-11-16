using LibraryHub.DTOs.Pagination;
using LibraryHub.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryHub.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(IUserRepository repo) : ControllerBase
{
    private readonly IUserRepository _repo = repo;

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams query)
    {
        var users = await _repo.GetAllAsync(query);
        return Ok(users);
    }

    [HttpPost("{id}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole(string id, [FromBody] RoleRequestDto request)
    {
        var result = await _repo.AssignRoleAsync(
            id, request.Role);
        return Ok(result);
    }
    public class RoleRequestDto
    {
        public string Role { get; set; } = string.Empty;
    }

    [HttpDelete("{id}/roles/{role}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveRole(string id, string role)
    {
        var result = await _repo.RemoveRoleAsync(id, role);
        return Ok(result);
    }
}
