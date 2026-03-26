using ACTProAPI.Helpers;
using ACTServiceReference;
using System.ServiceModel;

namespace ACTProAPI.Services
{
    public class ACTProServices : IACTProServices
    {
        private readonly ILogger<ACTProServices> _logger;
        private readonly SettingsHelper _settings;

        public string actServer;
        public string actUsername;
        public string actPassword;
        public string appName;
        public ActEnterprisePublicAPI_ExtClient proxy = null;


        public ACTProServices(ILogger<ACTProServices> logger, SettingsHelper settings)
        {
                
            _logger = logger;
            _settings = settings;

            // Initialize ACT server connection details from settings
            actServer = _settings.actServer;
            actUsername = _settings.userName;
            actPassword = _settings.password;
            appName = _settings.appName;
        }

        public async Task CreateProxy()
        {
            NetTcpBinding binding = new NetTcpBinding(SecurityMode.Transport) 
            {
                SendTimeout = TimeSpan.FromSeconds(10),
                ReceiveTimeout = TimeSpan.FromSeconds(10),
                OpenTimeout = TimeSpan.FromSeconds(10),
                CloseTimeout = TimeSpan.FromSeconds(10)
            };

            //binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;

            EndpointAddress endpointAddress = new EndpointAddress($"net.tcp://{actServer}/ActEnterprisePublicUintAPI");

            try
            {
                proxy = new ActEnterprisePublicAPI_ExtClient(binding, endpointAddress);
                uint status = await proxy.EstablishPublicSessionAsync(actUsername,actPassword , appName, System.Environment.MachineName, "RVMS"); 
                

                if (status == 0)
                {
                    _logger.LogError("Failed to establish session");
                    throw new InvalidOperationException("ACT session returned status 0.");
                }
                else
                {
                    _logger.LogInformation("Session established successfully");
                  
                }
            }
            catch (CommunicationException ex)
            {
                proxy?.Abort();
                _logger.LogError($"Communication error: {ex.Message}");
                throw;
            }
            catch (TimeoutException ex)
            {

                proxy?.Abort();
                _logger.LogError($"Timeout: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                proxy?.Abort();
                _logger.LogError(ex.Message);
                throw;
            }

        }

        public async Task CloseProxy()
        {
            if (proxy != null)
            {
                try
                {
                    if (proxy.State == CommunicationState.Opened)
                    {
                        await proxy.ShutDownSessionAsync();
                        await proxy.CloseAsync();
                        _logger.LogInformation("Proxy closed successfully");
                    }
                    else
                    {
                        proxy.Abort();
                        _logger.LogWarning("Proxy was in {State} state — aborted", proxy.State);
                    }
                }
                catch (CommunicationException ex)
                {
                    proxy.Abort();
                    _logger.LogError($"Communication error while closing proxy: {ex.Message}");
                }
                catch (TimeoutException ex)
                {
                    proxy.Abort();
                    _logger.LogError($"Timeout while closing proxy: {ex.Message}");
                }
                catch (Exception ex)
                {
                    proxy.Abort();
                    _logger.LogError($"Error while closing proxy: {ex.Message}");
                }
                finally
                {
                    proxy = null;
                }
            }
        }
        public async Task AllowAccess(int globalDoorNumber)
        {

            if (proxy == null || proxy.State != CommunicationState.Opened)
                throw new InvalidOperationException("Proxy is not open. Cannot issue command.");

            CommandExt command = new CommandExt();
            command.Type = (uint)ACTCommandType.Door;
            command.DoorCommandInstruction = (uint)DoorCommands.ActivateRelay;
            //command.Controller = value.ControllerAddress;
            //command.Door = (byte)value.LocalDoorNumber;

            bool result = await proxy.IssueCommandOnDoorsAsync(command, [globalDoorNumber]);
            if (!result)
            {
                _logger.LogInformation($"Failed to activate relay on door {globalDoorNumber}");
            }
        }
    }
}
