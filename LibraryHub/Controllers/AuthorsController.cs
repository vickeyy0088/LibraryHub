using LibraryHub.DTOs.Author;
using LibraryHub.DTOs.Pagination;

// using LibraryHub.Helpers;
using LibraryHub.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryHub.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthorsController(IAuthorRepository repo) : ControllerBase
{
    private readonly IAuthorRepository _repo = repo;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams query)
    {
        var result = await _repo.GetAllAsync(query);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _repo.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Create([FromBody] AuthorCreateDto request)
    {
        var author = await _repo.CreateAsync(request);
        return Ok(author);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Update(int id, [FromBody] AuthorUpdateDto request)
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
