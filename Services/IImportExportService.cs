using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Service for ACT import/export operations.</summary>
public interface IImportExportService
{
    /// <summary>Imports users from a CSV file stream.</summary>
    Task<int> ImportUsersAsync(ActEnterprisePublicAPI_ExtClient proxy, Stream csvStream);

    /// <summary>Exports all users to a CSV byte array.</summary>
    Task<byte[]> ExportUsersAsync(ActEnterprisePublicAPI_ExtClient proxy);
}
