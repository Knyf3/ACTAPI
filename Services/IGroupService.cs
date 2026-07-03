using ACTApi.DTOs;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Service for ACT door and user group management.</summary>
public interface IGroupService
{
    /// <summary>Gets all door groups.</summary>
    Task<List<GroupDto>> GetDoorGroupsAsync(ActEnterprisePublicAPI_ExtClient proxy);

    /// <summary>Gets a single door group.</summary>
    Task<GroupDto?> GetDoorGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, int id);

    /// <summary>Creates a new door group.</summary>
    Task<int> CreateDoorGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, GroupDto group);

    /// <summary>Updates an existing door group.</summary>
    Task UpdateDoorGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, GroupDto group);

    /// <summary>Deletes a door group.</summary>
    Task<bool> DeleteDoorGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, int id);

    /// <summary>Gets all user groups.</summary>
    Task<List<GroupDto>> GetUserGroupsAsync(ActEnterprisePublicAPI_ExtClient proxy);

    /// <summary>Gets a single user group.</summary>
    Task<GroupDto?> GetUserGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, int id);

    /// <summary>Creates a new user group.</summary>
    Task<int> CreateUserGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, GroupDto group);

    /// <summary>Updates an existing user group.</summary>
    Task UpdateUserGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, GroupDto group);

    /// <summary>Deletes a user group.</summary>
    Task<bool> DeleteUserGroupAsync(ActEnterprisePublicAPI_ExtClient proxy, int id);
}
