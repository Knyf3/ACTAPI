using ACTServiceReference;

namespace ACTApi.Services
{
    public interface IACTProServices
    {
        Task CreateProxy();
        Task CloseProxy();
        Task AllowAccess(int globalDoorNumber);

        /// <summary>Gets the current WCF proxy instance, or null if not connected.</summary>
        ActEnterprisePublicAPI_ExtClient? CurrentProxy { get; }
    }
}