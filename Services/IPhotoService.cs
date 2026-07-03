using ACTApi.DTOs;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Service for ACT user photo chunked read/write.</summary>
public interface IPhotoService
{
    /// <summary>Gets a user's photo as a byte array (JPEG). Returns null if no photo.</summary>
    Task<byte[]?> GetUserPhotoAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber);

    /// <summary>Sets a user's photo from a byte array (JPEG).</summary>
    Task<bool> SetUserPhotoAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber, byte[] photoData);

    /// <summary>Deletes a user's photo by overwriting with empty chunks.</summary>
    Task<bool> DeleteUserPhotoAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber);
}
