# LibraryHub

LibraryHub is a library management REST API built with ASP.NET Core and .NET 10.

The system supports:

- User registration and login
- JWT authentication
- Role-based authorization
- Authors
- Books
- Categories
- Members
- Loans and returns
- Popular book reports
- Member activity reports
- PostgreSQL persistence
- Redis distributed caching
- .NET Aspire orchestration
- Entity Framework Core migrations
- Unit/Integration tests

## Technology Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL
- Redis
- .NET Aspire
- ASP.NET Core Identity
- JWT Bearer Authentication
- xUnit
- FluentAssertions
- OpenTelemetry
- Docker

## Solution Structure

    LibraryHub/
    ├── LibraryHub/
    │   ├── Controllers/
    │   ├── Data/
    │   ├── DTOs/
    │   ├── Extensions/
    │   ├── Helpers/
    │   ├── Interfaces/
    │   ├── Models/
    │   ├── Services/
    │   ├── Shared/
    │   ├── Program.cs
    │   ├── appsettings.json
    │   └── LibraryHub.csproj
    │
    ├── LibraryHub_Aspire/
    │   ├── AppHost.cs
    │   └── LibraryHub_Aspire.csproj
    │
    ├── Migration_Services/
    │   └── Migration_Services.csproj
    │
    ├── IntegrationTest/
    │   ├── Controllers/
    │   ├── CustomWebApplicationFactory.cs
    │   └── IntegrationTests.csproj
    │
    ├── UnitTests/
    ├── ServiceDefaults/
    └── LibraryHub.slnx

## Requirements

Install the following tools:

- .NET 10 SDK
- Docker Desktop
- PostgreSQL, if running without Aspire
- Redis, if running without Aspire
- Visual Studio 2026 or another .NET-compatible IDE

Verify the installed .NET SDK:

    dotnet --version

The project targets:

    net10.0

## Configuration

The API requires:

- PostgreSQL connection string
- JWT key
- JWT issuer
- JWT audience
- JWT token expiration settings
- Allowed CORS origins

Configuration is located in:

    LibraryHub/appsettings.json

Example configuration:

    {
      "ConnectionStrings": {
        "libraryhub": "Host=localhost;Port=5432;Database=LibraryHubDb;Username=postgres;Password=your-password"
      },
      "Jwt": {
        "Key": "replace-this-with-a-long-secret-key",
        "Issuer": "LibraryHub.API",
        "Audience": "LibraryHub.Client",
        "AccessTokenExpirationMinutes": 15,
        "RefreshTokenExpirationDays": 7
      },
      "AllowedHosts": "*",
      "Cors": {
        "AllowedOrigins": [
          "https://localhost:3000"
        ]
      }
    }

Do not commit real passwords or JWT secrets to source control.

## Environment Variables

ASP.NET Core converts nested configuration keys into environment variables by replacing `:` with `__`.

### PowerShell

    $env:ConnectionStrings__libraryhub="Host=localhost;Port=5432;Database=LibraryHubDb;Username=postgres;Password=your-password"
    $env:Jwt__Key="replace-this-with-a-long-random-secret"
    $env:Jwt__Issuer="LibraryHub.API"
    $env:Jwt__Audience="LibraryHub.Client"
    $env:Jwt__AccessTokenExpirationMinutes="15"
    $env:Jwt__RefreshTokenExpirationDays="7"
    $env:Cors__AllowedOrigins__0="https://localhost:3000"

### Bash

    export ConnectionStrings__libraryhub="Host=localhost;Port=5432;Database=LibraryHubDb;Username=postgres;Password=your-password"
    export Jwt__Key="replace-this-with-a-long-random-secret"
    export Jwt__Issuer="LibraryHub.API"
    export Jwt__Audience="LibraryHub.Client"
    export Jwt__AccessTokenExpirationMinutes="15"
    export Jwt__RefreshTokenExpirationDays="7"
    export Cors__AllowedOrigins__0="https://localhost:3000"

### User Secrets

The API supports .NET User Secrets.

From the repository root:

    dotnet user-secrets --project LibraryHub/LibraryHub.csproj set "ConnectionStrings:libraryhub" "Host=localhost;Port=5432;Database=LibraryHubDb;Username=postgres;Password=your-password"

    dotnet user-secrets --project LibraryHub/LibraryHub.csproj set "Jwt:Key" "replace-this-with-a-long-random-secret"

    dotnet user-secrets --project LibraryHub/LibraryHub.csproj set "Jwt:Issuer" "LibraryHub.API"

    dotnet user-secrets --project LibraryHub/LibraryHub.csproj set "Jwt:Audience" "LibraryHub.Client"

## Running with .NET Aspire

The recommended way to run LibraryHub is with the Aspire AppHost.

The AppHost starts:

- PostgreSQL
- pgAdmin
- Redis
- RedisInsight
- Database migration service
- LibraryHub API

Run the Aspire host:

    dotnet run --project LibraryHub_Aspire/LibraryHub_Aspire.csproj

Or run `LibraryHub_Aspire` from Visual Studio.

Aspire automatically:

1. Starts PostgreSQL.
2. Creates the `libraryhub` database resource.
3. Starts Redis.
4. Runs the migration service.
5. Starts the API after dependencies are ready.

The Aspire dashboard URL will be shown in the terminal or Visual Studio output window.

## Running Without Aspire

If you run the API directly, PostgreSQL and Redis must already be running.

    dotnet run --project LibraryHub/LibraryHub.csproj

Make sure the following configuration values are available:

    ConnectionStrings:libraryhub
    ConnectionStrings:cache
    Jwt:Key
    Jwt:Issuer
    Jwt:Audience

## Database Migrations

Create a migration:

    dotnet ef migrations add MigrationName --project LibraryHub/LibraryHub.csproj --startup-project LibraryHub/LibraryHub.csproj

Apply migrations:

    dotnet ef database update --project LibraryHub/LibraryHub.csproj --startup-project LibraryHub/LibraryHub.csproj

When running through Aspire, the migration service applies the migrations before the API starts.

## Build the Solution

Restore dependencies:

    dotnet restore

Build the solution:

    dotnet build

Build only the API:

    dotnet build LibraryHub/LibraryHub.csproj

## Run Tests

Run all tests:

    dotnet test

Run integration tests:

    dotnet test IntegrationTest/IntegrationTests.csproj

Run unit tests:

    dotnet test UnitTests/UnitTests.csproj

The integration tests use:

- An in-memory Entity Framework database
- A custom authentication handler
- Test authentication roles
- `CustomWebApplicationFactory`

## Authentication

LibraryHub uses JWT Bearer authentication.

For protected endpoints, send the access token using:

    Authorization: Bearer YOUR_ACCESS_TOKEN

Example:

    curl -X GET "https://localhost:7000/api/auth/me" -H "Authorization: Bearer YOUR_ACCESS_TOKEN"

The exact port depends on the Aspire or Visual Studio launch profile.

## Roles

The application uses the following roles:

| Role | Description |
|---|---|
| `Member` | Regular library member |
| `Librarian` | Staff user who manages books, authors, categories, loans, and reports |
| `Admin` | Full administrative access, including user and role management |

### General Permissions

| Feature | Member | Librarian | Admin |
|---|---:|---:|---:|
| View authors | Yes | Yes | Yes |
| Create authors | No | Yes | Yes |
| Update authors | No | Yes | Yes |
| Delete authors | No | No | Yes |
| View books | Yes | Yes | Yes |
| Create books | No | Yes | Yes |
| Update books | No | Yes | Yes |
| Delete books | No | No | Yes |
| View categories | Yes | Yes | Yes |
| Create categories | No | Yes | Yes |
| Update categories | No | Yes | Yes |
| Delete categories | No | No | Yes |
| View members | No | Yes | Yes |
| Create members | No | Yes | Yes |
| Update members | No | Yes | Yes |
| Delete members | No | No | Yes |
| View loans | Yes | Yes | Yes |
| Create loans | No | Yes | Yes |
| Return loans | No | Yes | Yes |
| View reports | No | Yes | Yes |
| Manage users | No | No | Yes |

## API Base URL

Set the base URL according to the URL shown by Visual Studio or Aspire.

### PowerShell

    $BaseUrl = "https://localhost:7000"

### Bash

    export BASE_URL="https://localhost:7000"

Replace the port with the port shown by your environment.

# Authentication Endpoints

## Register

    POST /api/auth/register

Request:

    {
      "fullName": "John Smith",
      "email": "john.smith@example.com",
      "password": "Password@123",
      "role": "Member"
    }

PowerShell:

    $body = @{
        fullName = "John Smith"
        email = "john.smith@example.com"
        password = "Password@123"
        role = "Member"
    } | ConvertTo-Json

    Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/api/auth/register" `
        -ContentType "application/json" `
        -Body $body

Allowed registration roles are:

    Member
    Admin

A Librarian account may need to be assigned through the user-management functionality.

## Login

    POST /api/auth/login

Request:

    {
      "email": "john.smith@example.com",
      "password": "Password@123"
    }

PowerShell:

    $body = @{
        email = "john.smith@example.com"
        password = "Password@123"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/api/auth/login" `
        -ContentType "application/json" `
        -Body $body

    $AccessToken = $loginResponse.data.accessToken

Create the authorization header:

    $Headers = @{
        Authorization = "Bearer $AccessToken"
    }

## Get Current User

    GET /api/auth/me

    Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/api/auth/me" `
        -Headers $Headers

## Refresh Token

    POST /api/auth/refresh

Request:

    {
      "refreshToken": "YOUR_REFRESH_TOKEN"
    }

## Logout

    POST /api/auth/logout

Request:

    {
      "refreshToken": "YOUR_REFRESH_TOKEN"
    }

## Change Password

Requires authentication.

    POST /api/auth/change-password

Request:

    {
      "currentPassword": "Password@123",
      "newPassword": "NewPassword@123",
      "confirmNewPassword": "NewPassword@123"
    }

# Author Endpoints

## Get Authors

Requires authentication.

    GET /api/authors?pageNumber=1&pageSize=10

Search authors:

    GET /api/authors?pageNumber=1&pageSize=10&search=orwell

## Get Author by ID

    GET /api/authors/{id}

Example:

    GET /api/authors/1

## Create Author

Requires `Librarian` or `Admin`.

    POST /api/authors

Request:

    {
      "firstName": "George",
      "lastName": "Orwell",
      "biography": "English novelist and essayist."
    }

## Update Author

Requires `Librarian` or `Admin`.

    PUT /api/authors/{id}

Request:

    {
      "firstName": "George",
      "lastName": "Orwell",
      "biography": "Updated biography."
    }

## Delete Author

Requires `Admin`.

    DELETE /api/authors/{id}

# Book Endpoints

Book GET endpoints are public.

## Get Books

    GET /api/books?pageNumber=1&pageSize=10

Search books:

    GET /api/books?pageNumber=1&pageSize=10&search=clean

## Get Book by ID

    GET /api/books/{id}

## Create Book

Requires `Librarian` or `Admin`.

    POST /api/books

Request:

    {
      "title": "Clean Code",
      "isbn": "9780132350884",
      "price": 35.99,
      "totalCopies": 5,
      "authorId": 1,
      "categoryId": 1,
      "version": 0
    }

The book requires an existing author and category.

## Update Book

Requires `Librarian` or `Admin`.

    PUT /api/books/{id}

Request:

    {
      "title": "Clean Code - Updated",
      "isbn": "9780132350884",
      "price": 39.99,
      "totalCopies": 6,
      "authorId": 1,
      "categoryId": 1,
      "version": 0
    }

The `version` property is used for optimistic concurrency.

## Delete Book

Requires `Admin`.

    DELETE /api/books/{id}

# Category Endpoints

## Get Categories

Requires authentication.

    GET /api/categories?pageNumber=1&pageSize=10

## Get Category by ID

    GET /api/categories/{id}

## Create Category

Requires `Librarian` or `Admin`.

    POST /api/categories

Request:

    {
      "name": "Technology",
      "description": "Technology and software development books."
    }

## Update Category

Requires `Librarian` or `Admin`.

    PUT /api/categories/{id}

Request:

    {
      "name": "Software Development",
      "description": "Software engineering and programming books."
    }

## Delete Category

Requires `Admin`.

    DELETE /api/categories/{id}

# Member Endpoints

## Get Members

Requires `Librarian` or `Admin`.

    GET /api/members?pageNumber=1&pageSize=10

## Get Member by ID

Requires authentication.

    GET /api/members/{id}

## Create Member

Requires `Librarian` or `Admin`.

The referenced application user must already exist.

    POST /api/members

Request:

    {
      "membershipNumber": "MEM-000001",
      "applicationUserId": "USER_ID",
      "membershipDate": "2026-01-01T00:00:00Z",
      "isActive": true
    }

## Update Member

Requires `Librarian` or `Admin`.

    PUT /api/members/{id}

Request:

    {
      "membershipNumber": "MEM-000001",
      "isActive": false
    }

## Delete Member

Requires `Admin`.

    DELETE /api/members/{id}

Members with loan records may not be deletable.

# Loan Endpoints

Loan endpoints require authentication.

## Get Loans

    GET /api/loans?pageNumber=1&pageSize=10

Behavior:

- Admin and Librarian users can view all loans.
- Other authenticated users can view their own loans.

## Create Loan

Requires `Librarian` or `Admin`.

The referenced book and member must exist, and the book must have available copies.

    POST /api/loans

Request:

    {
      "bookId": 1,
      "memberId": 1,
      "dueDate": "2026-12-31T00:00:00Z"
    }

## Return Loan

Requires `Librarian` or `Admin`.

    PUT /api/loans/{id}/return

Example:

    PUT /api/loans/1/return

## Get Overdue Loans

Requires `Librarian` or `Admin`.

    GET /api/loans/overdue

# Report Endpoints

Reports require `Librarian` or `Admin`.

## Popular Books Report

    GET /api/reports/popular-books?pageNumber=1&pageSize=10&limit=10

Optional search:

    GET /api/reports/popular-books?pageNumber=1&pageSize=10&limit=10&search=clean

## Member Activity Report

    GET /api/reports/member-activity?pageNumber=1&pageSize=10&limit=10

Optional search:

    GET /api/reports/member-activity?pageNumber=1&pageSize=10&limit=10&search=MEM

# User Administration Endpoints

User-management endpoints require `Admin`.

## Get Users

    GET /api/users?pageNumber=1&pageSize=10

## Assign Role

    POST /api/users/{userId}/roles

Request:

    {
      "role": "Librarian"
    }

Example:

    POST /api/users/USER_ID/roles

## Remove Role

    DELETE /api/users/{userId}/roles/{role}

Example:

    DELETE /api/users/USER_ID/roles/Librarian

# Standard Pagination

Most list endpoints support:

| Parameter | Description | Default |
|---|---|---:|
| `pageNumber` | Page number | `1` |
| `pageSize` | Number of records per page | `5` |
| `search` | Optional search text | Empty |
| `limit` | Report result limit | `10` |

Example:

    GET /api/books?pageNumber=2&pageSize=20&search=programming

# Standard API Response

Successful responses generally use:

    {
      "success": true,
      "message": "Operation completed successfully.",
      "data": {}
    }

Failed responses generally use:

    {
      "success": false,
      "message": "An error occurred.",
      "data": null
    }

# Caching

LibraryHub uses Redis distributed caching through `IDistributedCache`.

The cache helper provides:

- `GetOrSetAsync`
- `RemoveAsync`

Cached entries use expiration settings and are automatically removed by Redis when their expiration time is reached.

Individual cache entries can also be explicitly removed after update or delete operations.

# Rate Limiting

Registration and login endpoints use the configured rate-limiting policy.

The following endpoints are rate limited:

    POST /api/auth/register
    POST /api/auth/login

If the request limit is exceeded, the API returns:

    429 Too Many Requests

# Health and Aspire Endpoints

When running through Aspire, the application exposes default Aspire service endpoints through ServiceDefaults.

Use the Aspire dashboard to inspect:

- Application status
- Logs
- Traces
- Metrics
- PostgreSQL status
- Redis status
- RedisInsight

# Common Problems

## PostgreSQL Connection Error

Check that:

- PostgreSQL is running.
- The database exists.
- The username and password are correct.
- The `ConnectionStrings:libraryhub` value is correct.

Example:

    Host=localhost;Port=5432;Database=LibraryHubDb;Username=postgres;Password=your-password

## Redis Connection Error

Check that:

- Redis is running.
- Aspire has started the `cache` resource.
- The API receives the `cache` connection string.
- Docker is running when using Aspire.

## JWT Configuration Error

Make sure these settings exist:

    Jwt:Key
    Jwt:Issuer
    Jwt:Audience

The JWT key should be long and stored securely.

## Unauthorized Response

Check that:

1. You logged in successfully.
2. You copied the correct access token.
3. The request contains the authorization header.

Example:

    Authorization: Bearer YOUR_ACCESS_TOKEN

## Forbidden Response

A `403 Forbidden` response means the user is authenticated but does not have the required role.

Examples:

- Member attempting to create a book
- Librarian attempting to delete a book
- Librarian attempting to manage users

# Development Workflow

Restore dependencies:

    dotnet restore

Build the solution:

    dotnet build

Run tests:

    dotnet test

Run the Aspire application:

    dotnet run --project LibraryHub_Aspire/LibraryHub_Aspire.csproj

Run the API directly:

    dotnet run --project LibraryHub/LibraryHub.csproj

# Security Notes

- Do not commit production database passwords.
- Do not commit production JWT keys.
- Use User Secrets or environment variables for local development.
- Use a secrets manager in production.
- Use HTTPS for deployed environments.
- Use strong PostgreSQL and application-user passwords.
- Restrict CORS origins instead of allowing every origin.
- Rotate JWT signing keys when required.
- Do not expose pgAdmin or RedisInsight publicly in production without authentication and network restrictions.

Thank You....
