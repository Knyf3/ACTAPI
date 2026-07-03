using System.Diagnostics;
using System.ServiceModel;
using System.Text.Json;

namespace ACTApi.Middleware;

/// <summary>
/// Global exception handling middleware that maps WCF and application exceptions
/// to structured RFC 7807 Problem Details responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (EndpointNotFoundException ex)
        {
            _logger.LogError(ex, "ACT server unreachable at configured endpoint");
            await WriteProblemDetailsAsync(context, StatusCodes.Status503ServiceUnavailable,
                "ACT Server Unreachable",
                "The ACT Enterprise service could not be reached. Verify the server is running and the ACTHost/ACTPort settings are correct.",
                ex.Message);
        }
        catch (CommunicationObjectFaultedException ex)
        {
            _logger.LogWarning(ex, "WCF session entered faulted state — will be re-established on next request");
            await WriteProblemDetailsAsync(context, StatusCodes.Status503ServiceUnavailable,
                "WCF Session Faulted",
                "The connection to the ACT service entered a faulted state. The next request will establish a fresh session.",
                ex.Message);
        }
        catch (CommunicationException ex)
        {
            _logger.LogError(ex, "WCF communication failure");
            await WriteProblemDetailsAsync(context, StatusCodes.Status503ServiceUnavailable,
                "ACT Communication Error",
                "A communication error occurred with the ACT Enterprise service. Verify network connectivity.",
                ex.Message);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "WCF operation timed out");
            await WriteProblemDetailsAsync(context, StatusCodes.Status504GatewayTimeout,
                "ACT Operation Timeout",
                "The ACT service did not respond within the configured timeout. The operation may have succeeded server-side.",
                ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Proxy"))
        {
            _logger.LogWarning(ex, "Session proxy in invalid state");
            await WriteProblemDetailsAsync(context, StatusCodes.Status503ServiceUnavailable,
                "Session Not Ready",
                "The ACT session proxy was not in a valid state. The next request will establish a fresh session.",
                ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "ACT DBUser permission denied for operation");
            await WriteProblemDetailsAsync(context, StatusCodes.Status403Forbidden,
                "Permission Denied",
                "The configured ACT DBUser does not have permission to perform this operation.",
                ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters");
            await WriteProblemDetailsAsync(context, StatusCodes.Status400BadRequest,
                "Invalid Parameters",
                "The request contained invalid or missing parameters.",
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing request {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                null,
                null);
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string title,
        string? detail,
        string? exceptionMessage)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            Type = $"https://httpstatuses.io/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail ?? "An unexpected error occurred.",
            Exception = exceptionMessage,
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }
}
