var builder = DistributedApplication.CreateBuilder(args);

// 1. Create PostgreSQL server
var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume() // Attaches a persistent Docker volume to the PostgreSQL container
                      // so your data persists even if the container stops or restarts
    .WithPgAdmin();

// 2. Create LibraryHub database
var database = postgres.AddDatabase("libraryhub");

// 3. Create Redis cache resource
var cache = builder.AddRedis("cache")
    .WithDataVolume() // Attaches a persistent Docker volume to the Redis container
                      // so your data persists even if the container stops or restarts
    .WithRedisInsight(); // Optional: adds Redis GUI to Aspire Dashboard")

// 4. Run Migration Console App
var migrationService = builder
    .AddProject<Projects.Migration_Services>("migration-service")
    .WithReference(database)
    .WaitFor(database);

// 5. Start LibraryHub API after migration completes
builder
    .AddProject<Projects.LibraryHub>("libraryhub-api")
    .WithReference(database)
    .WithReference(cache)
    .WaitFor(cache)  // Waits for the Redis cache to be ready before starting the LibraryHub API
    .WaitForCompletion(migrationService);

// 6. Run Aspire
builder.Build().Run();