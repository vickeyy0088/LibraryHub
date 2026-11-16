using LibraryHub.DTOs.Category;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryHub.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController(ICategoryRepository _repo) : ControllerBase
{
    private readonly ICategoryRepository _repo = _repo;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams query)
    {
        var category = await _repo.GetAllAsync(query);
        return Ok(category);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _repo.GetByIdAsync(id);
        return Ok(category);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Create(
        [FromBody] CategoryCreateDto request)
    {
        var category = await _repo.CreateAsync(request);
        return Ok(category);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CategoryUpdateDto request)
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
