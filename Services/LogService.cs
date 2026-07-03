using ACTApi.DTOs;
using ACTApi.Infrastructure;
using ACTApi.Mappers;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Domain service for ACT log event queries.</summary>
public class LogService : ILogService
{
    private readonly ILogger<LogService> _logger;

    /// <summary>Initializes a new instance of <see cref="LogService"/>.</summary>
    public LogService(ILogger<LogService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<LogEventDto>> GetLogsAsync(
        ActEnterprisePublicAPI_ExtClient proxy,
        DateTime? from, DateTime? to, uint? eventType,
        int page = 1, int pageSize = 200)
    {
        var fromDate = from ?? DateTime.Today.AddDays(-1);
        var toDate = to ?? DateTime.Now;

        if (eventType.HasValue)
        {
            return await WcfPaginationHelper.GetPageAsync<LogValueExt, LogEventDto>(
                (start, max) => proxy.GetLogsOfEventTypeAsync(fromDate, toDate, eventType.Value, start, max, true),
                page,
                pageSize,
                l => l.ToDto());
        }

        // Default: get most recent log events
        if (page == 1 && pageSize <= 500)
        {
            var events = await WcfCallLogger.ExecuteAsync(
                () => proxy.GetMostRecentLogEventsAsync(pageSize),
                "GetMostRecentLogEvents",
                _logger);

            var items = events?.Select(e => e.ToDto()).ToList() ?? new List<LogEventDto>();

            return new PaginatedResponse<LogEventDto>
            {
                Data = items,
                TotalCount = items.Count,
                Page = page,
                PageSize = pageSize,
                HasMore = items.Count >= pageSize
            };
        }

        return new PaginatedResponse<LogEventDto>
        {
            Data = new List<LogEventDto>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize,
            HasMore = false
        };
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<LogEventDto>> GetLogsByUserAsync(
        ActEnterprisePublicAPI_ExtClient proxy,
        int userNumber, DateTime? from, DateTime? to,
        int page = 1, int pageSize = 200)
    {
        var fromDate = from ?? DateTime.Today.AddDays(-1);
        var toDate = to ?? DateTime.Now;

        return await WcfPaginationHelper.GetPageAsync<LogValueExt, LogEventDto>(
            (start, max) => proxy.GetLogsOfUserIDAsync(fromDate, toDate, userNumber, start, max, true),
            page,
            pageSize,
            l => l.ToDto());
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<LogEventDto>> GetLogsByDoorAsync(
        ActEnterprisePublicAPI_ExtClient proxy,
        int globalDoorNumber, DateTime? from, DateTime? to,
        int page = 1, int pageSize = 200)
    {
        var fromDate = from ?? DateTime.Today.AddDays(-1);
        var toDate = to ?? DateTime.Now;

        return await WcfPaginationHelper.GetPageAsync<LogValueExt, LogEventDto>(
            (start, max) => proxy.GetLogsOfGlobalDoorAsync(fromDate, toDate, globalDoorNumber, start, max, true),
            page,
            pageSize,
            l => l.ToDto());
    }
}
