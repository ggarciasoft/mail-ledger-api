using System.Diagnostics;
using System.Text;

namespace MainLedger.API.Middleware;

/// <summary>
/// Middleware to log HTTP requests and responses.
/// Can be enabled/disabled via configuration.
/// </summary>
public class HttpLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpLoggingMiddleware> _logger;
    private readonly bool _isEnabled;

    public HttpLoggingMiddleware(
        RequestDelegate next,
        ILogger<HttpLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _isEnabled = configuration.GetValue<bool>("Logging:EnableHttpLogging", false);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_isEnabled)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        // Log request
        await LogRequest(context, requestId);

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
            stopwatch.Stop();

            // Log response
            await LogResponse(context, requestId, stopwatch.ElapsedMilliseconds);

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, 
                "[{RequestId}] Request failed after {ElapsedMs}ms: {Method} {Path}",
                requestId, stopwatch.ElapsedMilliseconds, context.Request.Method, context.Request.Path);
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequest(HttpContext context, string requestId)
    {
        var request = context.Request;
        
        var logMessage = new StringBuilder();
        logMessage.AppendLine($"[{requestId}] HTTP Request:");
        logMessage.AppendLine($"  Method: {request.Method}");
        logMessage.AppendLine($"  Path: {request.Path}{request.QueryString}");
        logMessage.AppendLine($"  Headers:");
        
        foreach (var header in request.Headers)
        {
            // Don't log sensitive headers
            if (IsSensitiveHeader(header.Key))
            {
                logMessage.AppendLine($"    {header.Key}: [REDACTED]");
            }
            else
            {
                logMessage.AppendLine($"    {header.Key}: {header.Value}");
            }
        }

        // Log request body for POST/PUT/PATCH
        if (request.ContentLength > 0 && 
            (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH"))
        {
            request.EnableBuffering();
            var body = await ReadRequestBody(request);
            
            if (!string.IsNullOrEmpty(body))
            {
                logMessage.AppendLine($"  Body: {body}");
            }
            
            request.Body.Position = 0;
        }

        _logger.LogInformation(logMessage.ToString());
    }

    private async Task LogResponse(HttpContext context, string requestId, long elapsedMs)
    {
        var response = context.Response;
        
        var logMessage = new StringBuilder();
        logMessage.AppendLine($"[{requestId}] HTTP Response ({elapsedMs}ms):");
        logMessage.AppendLine($"  Status: {response.StatusCode}");
        logMessage.AppendLine($"  Headers:");
        
        foreach (var header in response.Headers)
        {
            logMessage.AppendLine($"    {header.Key}: {header.Value}");
        }

        // Log response body
        response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        if (!string.IsNullOrEmpty(responseBody) && responseBody.Length < 10000) // Limit size
        {
            logMessage.AppendLine($"  Body: {responseBody}");
        }
        else if (responseBody.Length >= 10000)
        {
            logMessage.AppendLine($"  Body: [Too large - {responseBody.Length} bytes]");
        }

        _logger.LogInformation(logMessage.ToString());
    }

    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        try
        {
            using var reader = new StreamReader(
                request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);
            
            var body = await reader.ReadToEndAsync();
            return body.Length > 10000 ? $"[Too large - {body.Length} bytes]" : body;
        }
        catch
        {
            return "[Unable to read body]";
        }
    }

    private bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "Authorization",
            "Cookie",
            "X-API-Key",
            "X-Auth-Token",
            "Set-Cookie"
        };

        return sensitiveHeaders.Any(h => 
            h.Equals(headerName, StringComparison.OrdinalIgnoreCase));
    }
}
