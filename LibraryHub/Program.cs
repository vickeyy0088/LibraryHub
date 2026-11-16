using LibraryHub.Extensions;
//using LibraryHub.Middleware;
using LibraryHub.Shared;
using ServiceDefaults;
//using Serilog;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry 
builder.AddServiceDefaults();

//// 1. Configure Serilog
//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Information()
//    .WriteTo.Console()
//    .WriteTo.File(
//        "Logs/libraryhub-.log",
//        rollingInterval: RollingInterval.Day)
//    .CreateLogger();

//builder.Services.AddSerilog();

// 2. Register global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 3. Register application services (DB, Identity, DI, JWT)
builder.Services.AddLibraryHubServices(builder.Configuration);

// 4. Register Redis Distributed Cache
builder.AddRedisDistributedCache("cache");

// 5. Register CORS
builder.Services.AddLibraryHubCors(builder.Configuration);

// Register Rate Limiting via Extension Method
builder.Services.AddCustomRateLimiting();

var app = builder.Build();

///  --------------------------------------------

app.UseExceptionHandler(_ => { });
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}
//app.UseMiddleware<AppLogging>();
app.UseHttpsRedirection();
app.UseCors("Externals");

// Keep Serilog request logging.
// This writes request events to Serilog's sinks.
// app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

public partial class Program { }

//// 5. Enable global exception handling
//app.UseExceptionHandler(_ => { });

//// 6. Log HTTP requests
//app.UseSerilogRequestLogging();

//// 7. Initialize database and apply pending migrations
////await app.Services.InitializeDatabaseAsync<ApplicationDbContext>();

////await using (var scope = app.Services.CreateAsyncScope())
////{
////    var db = scope.ServiceProvider
////        .GetRequiredService<ApplicationDbContext>();

////    await db.Database.MigrateAsync();
////}

//// 8. Seed Admin
////await app.SeedAdminAsync();

//// 9. Configure HTTP pipeline
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//app.UseHttpsRedirection();

//// 10. Enable CORS before authentication
//app.UseCors("Externals");

//// Enable Rate Limiter Middleware
//app.UseRateLimiter();

//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllers();

//app.Run();

