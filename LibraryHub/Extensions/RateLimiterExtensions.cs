using System.Threading.RateLimiting;

namespace LibraryHub.Extensions
{
    public static class RateLimiterExtensions
    {
        public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
        {
            return services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // for register and login endpoints
                options.AddPolicy("RateLimit", httpContext =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 5,
                                Window = TimeSpan.FromMinutes(15),
                                QueueLimit = 0
                            }));
            });
        }
    }
}
