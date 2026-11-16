using LibraryHub.DTOs.Book;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryHub.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BooksController(IBookRepository repo) : ControllerBase
{
    private readonly IBookRepository _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams query)
    {
        var books = await _repo.GetAllAsync(query);
        return Ok(books);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var book = await _repo.GetByIdAsync(id);
        return Ok(book);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Create(
        [FromBody] BookCreateDto request)
    {
        var book = await _repo.CreateAsync(request);
        return Ok(book);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] BookUpdateDto request)
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
