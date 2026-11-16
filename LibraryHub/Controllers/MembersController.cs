using LibraryHub.DTOs.Member;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryHub.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MembersController(IMemberRepository repo) : ControllerBase
{
    private readonly IMemberRepository _repo = repo;

    [HttpGet]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams query)
    {
        var members = await _repo.GetAllAsync(query);
        return Ok(members);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var member = await _repo.GetByIdAsync(id);
        return Ok(member);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Create(MemberCreateDto request)
    {
        var member = await _repo.CreateAsync(request);
        return Ok(member);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Update(
        int id,
        MemberUpdateDto request)
    {
        var updated = await _repo.UpdateAsync(id, request);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repo.DeleteAsync(id);
        return Ok(deleted);
    }
}
