using ACTApi.DTOs;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Service for ACT extra rights and door plan management.</summary>
public interface IExtraRightsService
{
    /// <summary>Gets extra rights for a user.</summary>
    Task<ExtraRightsDto?> GetExtraRightsAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber);

    /// <summary>Updates extra rights for a user.</summary>
    Task UpdateExtraRightsAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber, ExtraRightsDto rights);

    /// <summary>Deletes all extra rights for a user.</summary>
    Task<bool> DeleteExtraRightsAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber);

    /// <summary>Gets the door plan for a user.</summary>
    Task<DoorPlanDto?> GetDoorPlanAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber);

    /// <summary>Updates the door plan for a user.</summary>
    Task UpdateDoorPlanAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber, DoorPlanDto plan);

    /// <summary>Deletes the door plan for a user.</summary>
    Task<bool> DeleteDoorPlanAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber);
}
