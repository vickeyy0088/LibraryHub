using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security;

namespace LibraryHub.Shared;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            // 400 - Bad Request
            ArgumentException => StatusCodes.Status400BadRequest,

            // 401 - Unauthorized
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,

            // 403 - Forbidden
            SecurityException => StatusCodes.Status403Forbidden,

            // 404 - Not Found
            KeyNotFoundException or
            FileNotFoundException or
            DirectoryNotFoundException
                => StatusCodes.Status404NotFound,

            // 408 - Request Timeout
            TimeoutException => StatusCodes.Status408RequestTimeout,

            // 409 - Conflict
            DbUpdateConcurrencyException
                => StatusCodes.Status409Conflict,

            // 422 - Validation Error
            ValidationException
                => StatusCodes.Status422UnprocessableEntity,

            // 502 - Bad Gateway
            HttpRequestException
                => StatusCodes.Status502BadGateway,

            // 500 - Internal Server Error
            _ => StatusCodes.Status500InternalServerError
        };

        // Log concurrency conflicts as warnings
        if (exception is DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict occurred while processing {Path}",
                context.Request.Path);
        }
        else
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred while processing request {Path}. StatusCode: {StatusCode}, ExceptionType: {ExceptionType}",
                context.Request.Path,
                statusCode,
                exception.GetType().Name);
        }

        var message = exception switch
        {
            DbUpdateConcurrencyException =>
                "This record was modified by another user. Please refresh and try again.",

            _ when statusCode == StatusCodes.Status500InternalServerError =>
                "An unexpected error occurred. Please try again later.",

            _ => exception.Message
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(message);

        await context.Response.WriteAsJsonAsync(
            response,
            cancellationToken);

        return true;
    }
}




//using Microsoft.AspNetCore.Diagnostics;
//using Microsoft.EntityFrameworkCore;
//using System.ComponentModel.DataAnnotations;
//using System.Security;

//namespace LibraryHub.Shared;

//public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
//{
//    private readonly ILogger<GlobalExceptionHandler> _logger = logger;
//    public async ValueTask<bool> TryHandleAsync(
//        HttpContext context,
//        Exception exception,
//        CancellationToken cancellationToken)
//    {
//        var statusCode = exception switch
//        {
//            // 400 - Bad Request
//            ArgumentException or
//            ArgumentNullException or
//            ArgumentOutOfRangeException
//                => StatusCodes.Status400BadRequest,

//            // 401 - Unauthorized
//            UnauthorizedAccessException
//                => StatusCodes.Status401Unauthorized,

//            // 403 - Forbidden
//            SecurityException
//                => StatusCodes.Status403Forbidden,

//            // 404 - Not Found
//            KeyNotFoundException or
//            FileNotFoundException or
//            DirectoryNotFoundException
//                => StatusCodes.Status404NotFound,

//            // 408 - Request Timeout
//            TimeoutException
//                => StatusCodes.Status408RequestTimeout,

//            // 409 - Conflict
//            InvalidOperationException or
//            DbUpdateConcurrencyException
//                => StatusCodes.Status409Conflict,

//            // 422 - Validation Error
//            ValidationException
//                => StatusCodes.Status422UnprocessableEntity,

//            // 502 - Bad Gateway
//            HttpRequestException
//                => StatusCodes.Status502BadGateway,

//            // 500 - Internal Server Error
//            _ => StatusCodes.Status500InternalServerError
//        };

//        // Add logger for Error Globally
//        _logger.LogError(
//            exception,
//            "An unhandled exception occurred while processing request {Path}. StatusCode: {StatusCode}, ExceptionType: {ExceptionType}",
//            context.Request.Path,
//            statusCode,
//            exception.GetType().Name);

//        context.Response.StatusCode = statusCode;
//        context.Response.ContentType = "application/json";

//        var response = ApiResponse<object>.Fail(
//            exception.Message);

//        await context.Response.WriteAsJsonAsync(
//            response,
//            cancellationToken);

//        return true;
//    }
//}
