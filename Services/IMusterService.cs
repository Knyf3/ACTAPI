using ACTApi.DTOs;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Service for ACT muster (user tracking) operations.</summary>
public interface IMusterService
{
    /// <summary>Gets the current muster report (users on site).</summary>
    Task<List<UserTrackDto>> GetMusterAsync(ActEnterprisePublicAPI_ExtClient proxy);

    /// <summary>Gets the list of absent users.</summary>
    Task<List<LogEventDto>> GetAbsentUsersAsync(ActEnterprisePublicAPI_ExtClient proxy);

    /// <summary>Resets the muster data.</summary>
    Task<bool> ResetMusterAsync(ActEnterprisePublicAPI_ExtClient proxy);
}
