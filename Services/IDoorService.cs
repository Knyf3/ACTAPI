using ACTApi.DTOs;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Service for ACT door listing and commands.</summary>
public interface IDoorService
{
    /// <summary>Gets a paginated list of doors.</summary>
    Task<PaginatedResponse<DoorDto>> GetDoorsAsync(ActEnterprisePublicAPI_ExtClient proxy, int page = 1, int pageSize = 200);

    /// <summary>Gets a single door by its global door number.</summary>
    Task<DoorDto?> GetDoorAsync(ActEnterprisePublicAPI_ExtClient proxy, int globalDoorNumber);

    /// <summary>Issues a door command on a single door.</summary>
    Task<bool> IssueDoorCommandAsync(ActEnterprisePublicAPI_ExtClient proxy, int globalDoorNumber, byte command);

    /// <summary>Issues a door command on multiple doors.</summary>
    Task<bool> IssueDoorCommandBatchAsync(ActEnterprisePublicAPI_ExtClient proxy, int[] globalDoorNumbers, byte command);
}
