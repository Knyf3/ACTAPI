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
        public ActEnterprisePublicAPI_ExtClient proxy = null;


        public ACTProServices(ILogger<ACTProServices> logger, SettingsHelper settings)
        {
                
            _logger = logger;
            _settings = settings;

            // Initialize ACT server connection details from settings
            actServer = _settings.actServer;
            actUsername = _settings.userName;
            actPassword = _settings.password;

        }

        public async Task CreateProxy()
        {
            System.ServiceModel.Channels.Binding binding = new NetTcpBinding(SecurityMode.Transport) 
            {
                SendTimeout = TimeSpan.FromSeconds(10),
                ReceiveTimeout = TimeSpan.FromSeconds(10),
                OpenTimeout = TimeSpan.FromSeconds(10),
                CloseTimeout = TimeSpan.FromSeconds(10)
            };

            EndpointAddress endpointAddress = new EndpointAddress($"net.tcp://{actServer}/ActEnterprisePublicUintAPI");

            try
            {
                proxy = new ActEnterprisePublicAPI_ExtClient(binding, endpointAddress);
                uint status = await proxy.EstablishPublicSessionAsync(actUsername,actPassword , System.Environment.UserName, System.Environment.MachineName, "RVMS"); 
                

                if (status == 0)
                {
                    _logger.LogError("Failed to establish session");
                    
                }
                else
                {
                    _logger.LogInformation("Session established successfully");
                    
                }
            }
            catch (CommunicationException ex)
            {
                _logger.LogError($"Communication error: {ex.Message}");
                
            }
            catch (TimeoutException ex)
            {
                _logger.LogError($"Timeout: {ex.Message}");
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                
            }

        }

        public async Task CloseProxy()
        {
            if (proxy != null)
            {
                try
                {
                    await proxy.ShutDownSessionAsync();
                    await proxy.CloseAsync();
                    _logger.LogInformation("Proxy closed successfully");
                }
                catch (CommunicationException ex)
                {
                    _logger.LogError($"Communication error while closing proxy: {ex.Message}");
                }
                catch (TimeoutException ex)
                {
                    _logger.LogError($"Timeout while closing proxy: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error while closing proxy: {ex.Message}");
                }
            }
        }
        public async Task AllowAccess(int globalDoorNumber)
        {
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
