using LibraryHub.DTOs.Loan;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibraryHub.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LoansController(ILoanRepository loanRepository) : ControllerBase
{
    private readonly ILoanRepository _loanRepository = loanRepository;

    // Staff: all loans; other users: own loans only
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams query)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        bool isStaff =
            User.IsInRole("Admin") ||
            User.IsInRole("Librarian");

        var loans = await _loanRepository.GetAllAsync(
            userId, isStaff, query);

        return Ok(loans);
    }

    // Admin and Librarian only
    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Create(LoanCreateDto request)
    {
        var loan = await _loanRepository.CreateAsync(request);
        return Ok(loan);
    }

    // Admin and Librarian only
    [HttpPut("{id:int}/return")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Return(int id)
    {
        var returned = await _loanRepository.ReturnAsync(id);
        return Ok(returned);
    }

    // Admin and Librarian only
    [HttpGet("overdue")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetOverdue()
    {
        var loans = await _loanRepository.GetOverdueAsync();
        return Ok(loans);
    }
}
