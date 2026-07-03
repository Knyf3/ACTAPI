using ACTApi.DTOs;
using ACTApi.Infrastructure;
using ACTApi.Mappers;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Domain service for ACT muster (user tracking) operations.</summary>
public class MusterService : IMusterService
{
    private readonly ILogger<MusterService> _logger;

    /// <summary>Initializes a new instance of <see cref="MusterService"/>.</summary>
    public MusterService(ILogger<MusterService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<UserTrackDto>> GetMusterAsync(ActEnterprisePublicAPI_ExtClient proxy)
    {
        // Get tracking logs for today (type 0 = all events)
        var now = DateTime.Now;
        var todayStart = now.Date;
        var todayEnd = now.Date.AddDays(1);

        var tracks = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetLogsOfUserTrackingAsync(
                0, todayStart, todayEnd, 0, false, false, 0, false, true,
                0, 5000, true, 0),
            "GetLogsOfUserTracking",
            _logger);

        return tracks?.Select(t => t.ToDto()).ToList() ?? new List<UserTrackDto>();
    }

    /// <inheritdoc />
    public async Task<List<LogEventDto>> GetAbsentUsersAsync(ActEnterprisePublicAPI_ExtClient proxy)
    {
        var now = DateTime.Now;
        var todayStart = now.Date;
        var todayEnd = now.Date.AddDays(1);

        var absent = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetListOfAbsentUsersAsync(
                todayStart, todayEnd, 0, false, false, 0, false, true,
                0, true),
            "GetListOfAbsentUsers",
            _logger);

        return absent?.Select(a => a.ToDto()).ToList() ?? new List<LogEventDto>();
    }

    /// <inheritdoc />
    public async Task<bool> ResetMusterAsync(ActEnterprisePublicAPI_ExtClient proxy)
    {
        var result = await WcfCallLogger.ExecuteAsync(
            () => proxy.MusterResetAsync(),
            "MusterReset",
            _logger);

        _logger.LogInformation("Muster reset — result: {Result}", result);
        return result;
    }
}
