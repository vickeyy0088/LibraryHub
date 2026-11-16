using LibraryHub.Data;
using LibraryHub.Filters;
using LibraryHub.Helpers;
using LibraryHub.Interfaces;
using LibraryHub.Models;
using LibraryHub.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

//   ====================  database connection, Identity, JWT authentication, and dependency injection. =========================

namespace LibraryHub.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLibraryHubServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL
        //services.AddDbContext<ApplicationDbContext>(options =>
        //    options.UseNpgsql(
        //        configuration.GetConnectionString("cs")));

        // Aspire Postgres Useage.
        var connectionString = configuration.GetConnectionString("libraryhub")
            ?? throw new InvalidOperationException("Connection string 'libraryhub' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // JWT service
        services.AddScoped<IJwtService, JwtService>();

        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT Key is not configured.");

        var jwtIssuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT Issuer is not configured.");

        var jwtAudience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT Audience is not configured.");


        // JWT Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme =
                JwtBearerDefaults.AuthenticationScheme;

            options.DefaultChallengeScheme =
                JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,

                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey))
                };

            // Custom response when authentication fails
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    // Prevent the default challenge response
                    context.HandleResponse();

                    context.Response.StatusCode =
                        StatusCodes.Status401Unauthorized;

                    context.Response.ContentType =
                        "application/json";

                    var response = new
                    {
                        statusCode = 401,
                        error = "Unauthorized",
                        message =
                            "You are unauthorized. Please provide a valid access token."
                    };

                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(response));
                }
            };
        });


        // Controllers and OpenAPI
        services.AddScoped<RequestLoggingActionFilter>();

        services.AddControllers(options =>
        {
            options.Filters.AddService<RequestLoggingActionFilter>();
        });

        services.AddOpenApi();

        // Repositories and services
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ICacheHelper, CacheHelper>();

        return services;
    }
}