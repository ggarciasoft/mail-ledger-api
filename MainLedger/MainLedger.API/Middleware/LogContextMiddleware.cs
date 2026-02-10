using log4net;

namespace MainLedger.API.Middleware;

/// <summary>
/// Middleware to add HTTP context information to log4net context for error logging.
/// </summary>
public class LogContextMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ILog _logger = LogManager.GetLogger(typeof(LogContextMiddleware));

    public LogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Add HTTP context to log4net context
            LogicalThreadContext.Properties["request_path"] = context.Request.Path;
            LogicalThreadContext.Properties["request_method"] = context.Request.Method;
            
            // Add user ID if authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst("sub")?.Value 
                    ?? context.User.FindFirst("userId")?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    LogicalThreadContext.Properties["user_id"] = userId;
                }
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the exception with context
            _logger.Error($"Unhandled exception in request {context.Request.Method} {context.Request.Path}", ex);
            throw;
        }
        finally
        {
            // Clear context properties
            LogicalThreadContext.Properties.Remove("request_path");
            LogicalThreadContext.Properties.Remove("request_method");
            LogicalThreadContext.Properties.Remove("user_id");
        }
    }
}

/// <summary>
/// Extension method to register the LogContextMiddleware.
/// </summary>
public static class LogContextMiddlewareExtensions
{
    public static IApplicationBuilder UseLogContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LogContextMiddleware>();
    }
}
