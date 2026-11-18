using LibraryHub.Data;
using LibraryHub.DTOs.Book;
using LibraryHub.DTOs.Pagination;
using LibraryHub.Extensions;
using LibraryHub.Helpers;
using LibraryHub.Interfaces;
using LibraryHub.Models;
using LibraryHub.Shared;
using Microsoft.EntityFrameworkCore;

namespace LibraryHub.Services;

public class BookRepository(
    ApplicationDbContext context,
    ILogger<BookRepository> logger,
    ICacheHelper cache) : IBookRepository
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<BookRepository> _logger = logger;
    private readonly ICacheHelper _cache = cache;
    private const string BookByIdPrefix = "books:id:";

    public async Task<ApiResponse<PaginationResponseDto<BookResponseDto>>> GetAllAsync(
        PaginationParams query)
    {
        if (query.PageNumber < 1 || query.PageSize < 1)
        {
            _logger.LogWarning("Invalid book pagination parameters.");
            return ApiResponse<PaginationResponseDto<BookResponseDto>>.Fail(
                ApiMessage.Common.InvalidPaginationParameters);
        }

        string search = query.Search?
            .Trim()
            .ToLowerInvariant() ?? string.Empty;

        string cacheKey =
            $"books:p:{query.PageNumber}:s:{query.PageSize}:q:{search}";

        var books = await _cache.GetOrSetAsync(
            cacheKey,
            async cancellationToken =>
            {
                _logger.LogInformation("Books not found in cache. Loading from database. Page: {PageNumber}",
                    query.PageNumber);

                var result = _context.Books
                     .AsNoTracking()
                     .OrderBy(b => b.Id)
                     .WhereSearch(search, b => b.Title, b => b.ISBN)
                     .Select(b => new BookResponseDto
                     {
                         Id = b.Id,
                         Title = b.Title,
                         ISBN = b.ISBN,
                         Price = b.Price,
                         TotalCopies = b.TotalCopies,
                         AvailableCopies = b.AvailableCopies,
                         CreatedAt = b.CreatedAt,
                         AuthorId = b.AuthorId,
                         AuthorName = b.Author.FirstName + " " + b.Author.LastName,
                         CategoryId = b.CategoryId,
                         CategoryName = b.Category.Name,
                         //Version = b.Version
                     });
                return await result.ToPaginatedResponseAsync(
                    query.PageNumber, query.PageSize);
            },
            absoluteExpiration: TimeSpan.FromMinutes(1));

        return ApiResponse<PaginationResponseDto<BookResponseDto>>.Ok(
            ApiMessage.Books.GetAll, books);
    }

    public async Task<ApiResponse<BookResponseDto?>> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid book ID: {BookId}", id);
            return ApiResponse<BookResponseDto?>.Fail(ApiMessage.Common.InvalidId);
        }

        string cacheKey = $"{BookByIdPrefix}{id}";

        var book = await _cache.GetOrSetAsync(
            cacheKey,
            async cancellationToken =>
            {
                _logger.LogInformation(
                    "Book not found in cache. Loading from database. BookId: {BookId}",
                    id);
                var result = await _context.Books
                    .AsNoTracking()
                    .Where(b => b.Id == id)
                    .Select(b => new BookResponseDto
                    {
                        Id = b.Id,
                        Title = b.Title,
                        ISBN = b.ISBN,
                        Price = b.Price,
                        TotalCopies = b.TotalCopies,
                        AvailableCopies = b.AvailableCopies,
                        CreatedAt = b.CreatedAt,
                        AuthorId = b.AuthorId,
                        AuthorName = b.Author.FirstName + " " + b.Author.LastName,
                        CategoryId = b.CategoryId,
                        CategoryName = b.Category.Name,
                        //Version = b.Version
                    })
                    .FirstOrDefaultAsync(cancellationToken);
                return result;

            },
            absoluteExpiration: TimeSpan.FromMinutes(1),
            slidingExpiration: TimeSpan.FromMinutes(1));

        if (book == null)
        {
            _logger.LogWarning("Book not found. BookId: {BookId}", id);
            return ApiResponse<BookResponseDto?>.Fail(ApiMessage.Books.NotFound);
        }

        _logger.LogInformation("Book retrieved successfully. BookId: {BookId}", id);

        return ApiResponse<BookResponseDto?>.Ok(ApiMessage.Books.GetById, book);
    }

    public async Task<ApiResponse<BookResponseDto>> CreateAsync(BookCreateDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.ISBN))
        {
            _logger.LogWarning("Book creation failed. Title or ISBN is missing.");
            return ApiResponse<BookResponseDto>.Fail(ApiMessage.Books.TitleISBNRequired);
        }

        if (request.AuthorId <= 0 ||
            !await _context.Authors.AnyAsync(a => a.Id == request.AuthorId))
        {
            _logger.LogWarning("Author not found. AuthorId: {AuthorId}", request.AuthorId);
            return ApiResponse<BookResponseDto>.Fail(ApiMessage.Books.AuthorNotFound);
        }

        if (request.CategoryId <= 0 ||
            !await _context.Categories.AnyAsync(c => c.Id == request.CategoryId))
        {
            _logger.LogWarning("Category not found. CategoryId: {CategoryId}", request.CategoryId);
            return ApiResponse<BookResponseDto>.Fail(ApiMessage.Books.CategoryNotFound);
        }

        if (request.AvailableCopies > request.TotalCopies)
        {
            _logger.LogWarning("Available copies cannot exceed total copies. TotalCopies: {TotalCopies}, AvailableCopies: {AvailableCopies}", request.TotalCopies, request.AvailableCopies);
            return ApiResponse<BookResponseDto>.Fail(ApiMessage.Books.AvailableCopiesExceedTotal);
        }

        if (await _context.Books.AnyAsync(b => b.ISBN == request.ISBN))
        {
            _logger.LogWarning("Duplicate ISBN during book creation.");
            return ApiResponse<BookResponseDto>.Fail(ApiMessage.Books.ISBNExists);
        }

        var book = new Book
        {
            Title = request.Title,
            ISBN = request.ISBN,
            Price = request.Price,
            TotalCopies = request.TotalCopies,
            AvailableCopies = request.TotalCopies,
            AuthorId = request.AuthorId,
            CategoryId = request.CategoryId
        };

        await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();

        await _context.Entry(book).Reference(b => b.Author).LoadAsync();
        await _context.Entry(book).Reference(b => b.Category).LoadAsync();

        _logger.LogInformation(
            "Book created successfully. BookId: {BookId}, Title: {Title}",
            book.Id, book.Title);

        return ApiResponse<BookResponseDto>.Ok(
            ApiMessage.Books.Created, new BookResponseDto
            {
                Id = book.Id,
                Title = book.Title,
                ISBN = book.ISBN,
                Price = book.Price,
                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies,
                AuthorId = book.AuthorId,
                AuthorName = book.Author.FirstName + " " + book.Author.LastName,
                CategoryId = book.CategoryId,
                CategoryName = book.Category.Name,
                // Version = book.Version
            });
    }

    public async Task<ApiResponse<BookResponseDto>> UpdateAsync(
        int id, BookUpdateDto request)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid book ID for update: {BookId}", id);
            return ApiResponse<BookResponseDto>.Fail(ApiMessage.Common.InvalidId);
        }

        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            _logger.LogWarning("Book not found for update. BookId: {BookId}", id);
            return ApiResponse<BookResponseDto>.Fail(ApiMessage.Books.NotFound);
        }

        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.ISBN))
        {
            _logger.LogWarning("Book update failed. Title or ISBN is missing. BookId: {BookId}", id);
            return ApiResponse<BookResponseDto>.Fail(ApiMessage.Books.TitleISBNRequired);
        }

        if (request.AuthorId <= 0 ||
            !await _context.Authors.AnyAsync(a => a.Id == request.AuthorId))
        {
            _logger.LogWarning("Author not found. AuthorId: {AuthorId}", request.AuthorId);
            return ApiResponse<BookResponseDto>.Fail(ApiMessage.Books.AuthorNotFound);
        }

        if (request.CategoryId <= 0 ||
            !await _context.Categories.AnyAsync(c => c.Id == request.CategoryId))
        {
            _logger.LogWarning("Category not found. CategoryId: {CategoryId}", request.CategoryId);
            return ApiResponse<BookResponseDto>.Fail(ApiMessage.Books.CategoryNotFound);
        }

        if (await _context.Books.AnyAsync(b => b.ISBN == request.ISBN && b.Id != id))
        {
            _logger.LogWarning("Duplicate ISBN during book update. BookId: {BookId}", id);
            return ApiResponse<BookResponseDto>.Fail(ApiMessage.Books.ISBNExists);
        }

        var borrowedCopies = book.TotalCopies - book.AvailableCopies;

        if (request.TotalCopies < borrowedCopies)
        {
            _logger.LogWarning("Invalid total copies for BookId: {BookId}", id);
            return ApiResponse<BookResponseDto>.Fail(ApiMessage.Books.TotalCopiesInvalid);
        }

        // Optimistic concurrency check
        //_context.Entry(book)
        //    .Property(b => b.Version)
        //    .OriginalValue = request.Version;

        book.Title = request.Title;
        book.ISBN = request.ISBN;
        book.Price = request.Price;
        book.TotalCopies = request.TotalCopies;
        book.AvailableCopies = request.TotalCopies - borrowedCopies;
        book.AuthorId = request.AuthorId;
        book.CategoryId = request.CategoryId;

        await _context.SaveChangesAsync();

        await _context.Entry(book).Reference(b => b.Author).LoadAsync();
        await _context.Entry(book).Reference(b => b.Category).LoadAsync();

        _logger.LogInformation(
            "Book updated successfully. BookId: {BookId}, Title: {Title}",
            book.Id, book.Title);
        var Books = new BookResponseDto
        {
            Id = book.Id,
            Title = book.Title,
            ISBN = book.ISBN,
            Price = book.Price,
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies,
            AuthorId = book.AuthorId,
            AuthorName = book.Author.FirstName + " " + book.Author.LastName,
            CategoryId = book.CategoryId,
            CategoryName = book.Category.Name,

        };

        await _cache.RemoveAsync($"{BookByIdPrefix}{id}");

        return ApiResponse<BookResponseDto>.Ok(
            ApiMessage.Books.Updated, Books);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        if (id <= 0)
        {
            _logger.LogWarning("Invalid book ID for deletion: {BookId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Common.InvalidId);
        }

        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            _logger.LogWarning("Book not found for deletion. BookId: {BookId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Books.NotFound);
        }

        if (await _context.Loans.AnyAsync(l => l.BookId == id))
        {
            _logger.LogWarning("Book deletion blocked because loans exist. BookId: {BookId}", id);
            return ApiResponse<bool>.Fail(ApiMessage.Books.HasLoans);
        }

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Book deleted successfully. BookId: {BookId}, Title: {Title}",
            book.Id, book.Title);

        // Delete single item cache
        await _cache.RemoveAsync($"{BookByIdPrefix}{id}");
        return ApiResponse<bool>.Ok(ApiMessage.Books.Deleted, true);
    }
}
