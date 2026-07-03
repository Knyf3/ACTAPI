using System.Diagnostics;
using System.ServiceModel;

namespace ACTApi.Infrastructure;

/// <summary>
/// Wraps WCF service calls with structured logging of method name, duration, and errors.
/// Provides consistent observability across all ACT API operations.
/// </summary>
public static class WcfCallLogger
{
    /// <summary>
    /// Execute a WCF operation with structured logging.
    /// Logs the method name, duration, and any errors.
    /// </summary>
    public static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        ILogger logger)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            sw.Stop();
            logger.LogDebug("WCF {Operation} completed in {Duration}ms",
                operationName, sw.ElapsedMilliseconds);
            return result;
        }
        catch (FaultException ex)
        {
            sw.Stop();
            logger.LogError(ex, "WCF {Operation} failed after {Duration}ms with fault: {FaultReason}",
                operationName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
        catch (CommunicationException ex)
        {
            sw.Stop();
            logger.LogError(ex, "WCF {Operation} failed after {Duration}ms with communication error",
                operationName, sw.ElapsedMilliseconds);
            throw;
        }
        catch (TimeoutException ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "WCF {Operation} timed out after {Duration}ms",
                operationName, sw.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "WCF {Operation} threw unexpected exception after {Duration}ms",
                operationName, sw.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Execute a WCF void operation with structured logging.
    /// </summary>
    public static async Task ExecuteAsync(
        Func<Task> operation,
        string operationName,
        ILogger logger)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await operation();
            sw.Stop();
            logger.LogDebug("WCF {Operation} completed in {Duration}ms",
                operationName, sw.ElapsedMilliseconds);
        }
        catch (FaultException ex)
        {
            sw.Stop();
            logger.LogError(ex, "WCF {Operation} failed after {Duration}ms with fault: {FaultReason}",
                operationName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
        catch (CommunicationException ex)
        {
            sw.Stop();
            logger.LogError(ex, "WCF {Operation} failed after {Duration}ms with communication error",
                operationName, sw.ElapsedMilliseconds);
            throw;
        }
        catch (TimeoutException ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "WCF {Operation} timed out after {Duration}ms",
                operationName, sw.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "WCF {Operation} threw unexpected exception after {Duration}ms",
                operationName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
