using System.Diagnostics;

namespace ACTApi.Middleware;

/// <summary>
/// Logs every HTTP request with method, path, status code, duration, and client IP.
/// Essential for troubleshooting deployment issues and tracing consumer activity.
/// Also logs request bodies at Debug level for non-GET methods.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        // Determine client IP with X-Forwarded-For / X-Real-IP fallback
        var clientIp = GetClientIpAddress(context);

        // Capture request details before pipeline
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString;

        // Log request body for non-GET methods at Debug level
        if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method)
            && context.Request.ContentLength.HasValue
            && context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(
                context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Truncate long bodies to 4096 chars
            var truncated = body.Length > 4096
                ? body[..4096] + "..."
                : body;

            _logger.LogDebug(
                "HTTP {Method} {Path}{QueryString} request body [{ContentLength} bytes]: {Body}",
                method, path, queryString, body.Length, truncated);
        }

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var statusCode = context.Response.StatusCode;

            // Log at appropriate level based on status code
            if (statusCode >= 500)
            {
                _logger.LogError(
                    "HTTP {Method} {Path}{QueryString} responded {StatusCode} in {Duration}ms [Client: {ClientIp}]",
                    method, path, queryString, statusCode, sw.ElapsedMilliseconds, clientIp);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning(
                    "HTTP {Method} {Path}{QueryString} responded {StatusCode} in {Duration}ms [Client: {ClientIp}]",
                    method, path, queryString, statusCode, sw.ElapsedMilliseconds, clientIp);
            }
            else
            {
                _logger.LogInformation(
                    "HTTP {Method} {Path}{QueryString} responded {StatusCode} in {Duration}ms [Client: {ClientIp}]",
                    method, path, queryString, statusCode, sw.ElapsedMilliseconds, clientIp);
            }
        }
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check X-Forwarded-For header (comma-separated list, first is real client)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For",
                out var forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',')
                .FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(ip)) return ip;
        }

        // Check X-Real-IP header (single IP, commonly set by nginx)
        if (context.Request.Headers.TryGetValue("X-Real-IP",
                out var realIp))
        {
            var ip = realIp.FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(ip)) return ip;
        }

        // Fall back to direct remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
