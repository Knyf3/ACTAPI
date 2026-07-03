using ACTApi.DTOs;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Service for ACT log event queries.</summary>
public interface ILogService
{
    /// <summary>Gets logs with optional filters and pagination.</summary>
    Task<PaginatedResponse<LogEventDto>> GetLogsAsync(ActEnterprisePublicAPI_ExtClient proxy,
        DateTime? from, DateTime? to, uint? eventType, int page = 1, int pageSize = 200);

    /// <summary>Gets logs for a specific user.</summary>
    Task<PaginatedResponse<LogEventDto>> GetLogsByUserAsync(ActEnterprisePublicAPI_ExtClient proxy,
        int userNumber, DateTime? from, DateTime? to, int page = 1, int pageSize = 200);

    /// <summary>Gets logs for a specific door.</summary>
    Task<PaginatedResponse<LogEventDto>> GetLogsByDoorAsync(ActEnterprisePublicAPI_ExtClient proxy,
        int globalDoorNumber, DateTime? from, DateTime? to, int page = 1, int pageSize = 200);
}
