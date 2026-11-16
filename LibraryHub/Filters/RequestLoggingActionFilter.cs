using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace LibraryHub.Filters
{
    public class RequestLoggingActionFilter(ILogger<RequestLoggingActionFilter> logger) : IAsyncActionFilter
    {
        // runs before and after your controller action
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var actionName = context.ActionDescriptor.DisplayName;
            var stopwatch = Stopwatch.StartNew(); // measures how long the action takes

            logger.LogInformation("Starting execution of action {ActionName}", actionName);

            try
            {
                await next(); // allows the controller action to execute
            }
            finally
            {
                stopwatch.Stop();

                logger.LogInformation("Action {ActionName} executed in {ElapsedMilliseconds} ms",
                    actionName, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
