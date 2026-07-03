using ACTApi.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.ServiceModel;

namespace ACTApi.Controllers;

/// <summary>
/// Health check and diagnostics endpoints for the ACT API bridge service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly SettingsHelper _settings;

    public HealthController(ILogger<HealthController> logger, SettingsHelper settings)
    {
        _logger = logger;
        _settings = settings;
    }

    /// <summary>
    /// Basic health check — returns service status and configuration summary.
    /// Does NOT attempt to contact the ACT server.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var state = new
        {
            Status = "Running",
            Timestamp = DateTime.UtcNow,
            Service = "ACT API Bridge",
            Version = "1.0.0",
            ActServer = _settings.actServer,
            AppName = _settings.appName
        };

        _logger.LogInformation("Health check: service is running, ACT target: {ActServer}", _settings.actServer);
        return Ok(state);
    }

    /// <summary>
    /// Extended health check — attempts to establish a WCF session with the ACT server
    /// to verify end-to-end connectivity.
    /// </summary>
    [HttpGet("act")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CheckActConnection()
    {
        _logger.LogInformation("Attempting ACT connectivity check to {ActServer}", _settings.actServer);

        try
        {
            var binding = new NetTcpBinding(SecurityMode.Transport)
            {
                SendTimeout = TimeSpan.FromSeconds(10),
                ReceiveTimeout = TimeSpan.FromSeconds(10),
                OpenTimeout = TimeSpan.FromSeconds(10),
                CloseTimeout = TimeSpan.FromSeconds(10),
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferSize = int.MaxValue
            };
            binding.ReaderQuotas.MaxArrayLength = int.MaxValue;
            binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
            binding.ReaderQuotas.MaxBytesPerRead = int.MaxValue;
            binding.ReaderQuotas.MaxDepth = int.MaxValue;
            binding.ReaderQuotas.MaxNameTableCharCount = int.MaxValue;

            var endpoint = new EndpointAddress($"net.tcp://{_settings.actServer}/ActEnterprisePublicUintAPI");
            var proxy = new ACTServiceReference.ActEnterprisePublicAPI_ExtClient(binding, endpoint);

            _logger.LogDebug("Establishing test session with ACT server...");
            var status = await proxy.EstablishPublicSessionAsync(
                _settings.userName,
                _settings.password,
                _settings.appName,
                Environment.MachineName,
                "ACTAPI_HealthCheck");

            if (status == 1)
            {
                await proxy.ShutDownSessionAsync();
                await proxy.CloseAsync();
                _logger.LogInformation("ACT connectivity check: SUCCESS (session established and closed)");
                return Ok(new
                {
                    Status = "ACT Reachable",
                    SessionResult = "SuccessfulLogin",
                    SessionResultCode = status,
                    ActServer = _settings.actServer
                });
            }

            _logger.LogWarning("ACT connectivity check: session rejected with code {StatusCode}", status);
            proxy.Abort();
            return StatusCode(503, new
            {
                Status = "ACT Unreachable",
                SessionResultCode = status,
                Detail = "The ACT server rejected the session. Check credentials and licensing.",
                ActServer = _settings.actServer
            });
        }
        catch (EndpointNotFoundException ex)
        {
            _logger.LogError(ex, "ACT connectivity check: endpoint not found at {ActServer}", _settings.actServer);
            return StatusCode(503, new
            {
                Status = "ACT Unreachable",
                Detail = $"Cannot reach ACT server at {_settings.actServer}. Verify the server is running and the address is correct.",
                Error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ACT connectivity check: unexpected error");
            return StatusCode(503, new
            {
                Status = "ACT Unreachable",
                Detail = "An unexpected error occurred during connectivity check.",
                Error = ex.Message
            });
        }
    }
}
