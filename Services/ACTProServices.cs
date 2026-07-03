using ACTServiceReference;
using ACTApi.Helpers;
using ACTApi.Infrastructure;
using System.ServiceModel;

namespace ACTApi.Services
{
    public class ACTProServices : IACTProServices
    {
        private readonly ILogger<ACTProServices> _logger;
        private readonly SettingsHelper _settings;

        private string actServer;
        private string actUsername;
        private string actPassword;
        private string appName;
        private ActEnterprisePublicAPI_ExtClient? proxy;

        /// <summary>Gets the ACT server address.</summary>
        public string ActServer => actServer;

        /// <summary>Gets the application name registered with ACT.</summary>
        public string AppName => appName;

        /// <summary>Gets the current WCF proxy instance, or null if not connected.</summary>
        public ActEnterprisePublicAPI_ExtClient? CurrentProxy => proxy;

        /// <summary>True when the WCF proxy has an open session.</summary>
        public bool IsConnected =>
            proxy is not null && proxy.State == CommunicationState.Opened;

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
                CloseTimeout = TimeSpan.FromSeconds(10),
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferSize = int.MaxValue,
                ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas
                {
                    MaxArrayLength = int.MaxValue,
                    MaxStringContentLength = int.MaxValue,
                    MaxBytesPerRead = int.MaxValue,
                    MaxDepth = int.MaxValue,
                    MaxNameTableCharCount = int.MaxValue
                }
            };

            //binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;

            EndpointAddress endpointAddress =
                new EndpointAddress(
                    $"net.tcp://{actServer}/ActEnterprisePublicUintAPI");

            try
            {
                proxy = new ActEnterprisePublicAPI_ExtClient(binding, endpointAddress);

                uint status = await WcfCallLogger.ExecuteAsync(
                    () => proxy.EstablishPublicSessionAsync(
                        actUsername, actPassword, appName,
                        System.Environment.MachineName, "RVMS"),
                    "EstablishPublicSession",
                    _logger);

                if (status == 0)
                {
                    _logger.LogError(
                        "Failed to establish ACT session — server returned status 0");
                    throw new InvalidOperationException(
                        "ACT session returned status 0.");
                }

                _logger.LogInformation(
                    "ACT session established successfully for {AppName} on {ActServer}",
                    appName, actServer);
            }
            catch (Exception)
            {
                proxy?.Abort();
                proxy = null;
                throw;
            }
        }

        public async Task CloseProxy()
        {
            if (proxy is null)
                return;

            try
            {
                if (proxy.State == CommunicationState.Opened)
                {
                    await WcfCallLogger.ExecuteAsync(
                        () => proxy.ShutDownSessionAsync(),
                        "ShutDownSession",
                        _logger);

                    await WcfCallLogger.ExecuteAsync(
                        () => proxy.CloseAsync(),
                        "CloseProxy",
                        _logger);

                    _logger.LogInformation("Proxy closed successfully");
                }
                else
                {
                    proxy.Abort();
                    _logger.LogWarning(
                        "Proxy was in {State} state — aborted", proxy.State);
                }
            }
            catch (CommunicationException ex)
            {
                proxy.Abort();
                _logger.LogError(
                    ex,
                    "Communication error while closing proxy: {Message}",
                    ex.Message);
            }
            catch (TimeoutException ex)
            {
                proxy.Abort();
                _logger.LogError(
                    ex,
                    "Timeout while closing proxy: {Message}",
                    ex.Message);
            }
            catch (Exception ex)
            {
                proxy.Abort();
                _logger.LogError(
                    ex,
                    "Error while closing proxy: {Message}",
                    ex.Message);
            }
            finally
            {
                proxy = null;
            }
        }

        public async Task AllowAccess(int globalDoorNumber)
        {
            if (proxy is null || proxy.State != CommunicationState.Opened)
                throw new InvalidOperationException(
                    "Proxy is not open. Cannot issue command.");

            CommandExt command = new CommandExt
            {
                Type = (uint)ACTCommandType.Door,
                DoorCommandInstruction = (uint)DoorCommands.ActivateRelay
            };
            //command.Controller = value.ControllerAddress;
            //command.Door = (byte)value.LocalDoorNumber;

            bool result = await WcfCallLogger.ExecuteAsync(
                () => proxy.IssueCommandOnDoorsAsync(command, [globalDoorNumber]),
                "IssueCommandOnDoors",
                _logger);

            if (!result)
            {
                _logger.LogWarning(
                    "Failed to activate relay on door {GlobalDoorNumber}",
                    globalDoorNumber);
            }
        }
    }
}
