namespace ACTProAPI.Services
{
    public interface IACTProServices
    {
        Task CreateProxy();
        Task CloseProxy();
        Task AllowAccess(int globalDoorNumber);

    }
}