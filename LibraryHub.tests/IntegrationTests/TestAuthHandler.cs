using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace IntegrationTests;

public class TestAuthHandler(   // accept 3 dependencies from DI container (options, logger, encoder)
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    // Defines a custom authentication scheme name so we can use it in our integration tests
    public const string SchemeName = "TestScheme";
    // Defines a custom HTTP header name so individual tests can dynamically tell the handler which role (Admin, Librarian, Member
    public const string RoleHeader = "X-Test-Role";
    // Defines a custom HTTP header name so individual tests can dynamically tell the handler which user ID to use
    public const string UserIdHeader = "X-Test-UserId";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If no Authorization header is sent, return NoResult so ASP.NET returns 401 Unauthorized
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = Request.Headers.TryGetValue(UserIdHeader, out var idHeader) &&
                     !string.IsNullOrWhiteSpace(idHeader)
            ? idHeader.ToString()
            : "test-user-123";

        var claims = new List<Claim> // Default Data
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "testuser@libraryhub.com"),
            new(ClaimTypes.Email, "testuser@libraryhub.com")
        };

        // Attach role if passed in X-Test-Role header (e.g., "Admin", "Librarian", "Member")
        if (Request.Headers.TryGetValue(RoleHeader, out var roleHeader) &&
            !string.IsNullOrWhiteSpace(roleHeader))
        {
            claims.Add(new Claim(ClaimTypes.Role, roleHeader.ToString()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName); // it sets identity.IsAuthenticated = true
        var principal = new ClaimsPrincipal(identity); // Wraps the identity into the HttpContext.User object that controllers and minimal APIs read.
        var ticket = new AuthenticationTicket(principal, SchemeName); // Packages the principal with the scheme name that authenticated it.

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
