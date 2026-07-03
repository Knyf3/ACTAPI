using ACTApi.DTOs;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Service for ACT user CRUD operations.</summary>
public interface IUserService
{
    /// <summary>Gets a single user by their user number.</summary>
    Task<UserDto?> GetUserAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber);

    /// <summary>Searches users with optional filters and pagination.</summary>
    Task<PaginatedResponse<UserDto>> GetUsersAsync(ActEnterprisePublicAPI_ExtClient proxy, UserSearchRequest? request);

    /// <summary>Creates a new user and returns the assigned user number.</summary>
    Task<int> CreateUserAsync(ActEnterprisePublicAPI_ExtClient proxy, UserDto user);

    /// <summary>Updates an existing user.</summary>
    Task UpdateUserAsync(ActEnterprisePublicAPI_ExtClient proxy, UserDto user);

    /// <summary>Deletes a user by user number. Returns true if successful.</summary>
    Task<bool> DeleteUserAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber);
}
