using Newtonsoft.Json.Linq;

namespace ACTApi.Helpers
{
    /// <summary>
    /// Reads all application settings from the single Settings/Settings.json file.
    /// This is the ONLY configuration source — no appsettings.json overrides.
    /// </summary>
    public class SettingsHelper
    {
        public static JObject SettingsConfig { get; set; } = new();
        public string sharedSettingsPath { get; set; }

        // ── HTTP / ACT Server ───────────────────────────────────────────
        public string serverAddress { get; set; } = "http://localhost:8021";
        public string actServer { get; set; } = "localhost:8004";

        // ── ACT Credentials ─────────────────────────────────────────────
        public string userName { get; set; } = "";
        public string password { get; set; } = "";
        public string appName { get; set; } = "ACT_API_Bridge";

        // ── Feature Flags ───────────────────────────────────────────────
        public bool swaggerEnabled { get; set; } = true;
        public string corsOrigins { get; set; } = "https://knyf3.github.io";

        // ── Logging ─────────────────────────────────────────────────────
        public string logLevelDefault { get; set; } = "Information";
        public string logLevelACTApi { get; set; } = "Information";

        public SettingsHelper()
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Try Settings\Settings.json (deployment path)
            sharedSettingsPath = Path.GetFullPath(
                Path.Combine(exeDirectory, "Settings", "Settings.json"));

            if (!File.Exists(sharedSettingsPath))
            {
                // Fallback: project root (running from Visual Studio)
                sharedSettingsPath = Path.GetFullPath(
                    Path.Combine(exeDirectory, "..", "..", "..", "Settings", "Settings.json"));
            }

            if (!File.Exists(sharedSettingsPath))
            {
                // Last fallback: next to the EXE
                sharedSettingsPath = Path.GetFullPath(
                    Path.Combine(exeDirectory, "Settings.json"));
            }

            if (!File.Exists(sharedSettingsPath))
            {
                // No settings file — use all defaults
                return;
            }

            try
            {
                SettingsConfig = JObject.Parse(System.IO.File.ReadAllText(sharedSettingsPath));

                serverAddress = SettingsConfig["Server"]?.ToString() ?? serverAddress;
                actServer = SettingsConfig["ACTServer"]?.ToString() ?? actServer;
                userName = SettingsConfig["ACTUsername"]?.ToString() ?? userName;
                password = SettingsConfig["ACTPassword"]?.ToString() ?? password;
                appName = SettingsConfig["AppName"]?.ToString() ?? appName;
                swaggerEnabled = SettingsConfig["SwaggerEnabled"]?.Value<bool>() ?? swaggerEnabled;
                corsOrigins = SettingsConfig["CorsOrigins"]?.ToString() ?? corsOrigins;

                // Read Logging levels from the nested Logging:LogLevel section
                var logLevel = SettingsConfig["Logging"]?["LogLevel"];
                if (logLevel != null)
                {
                    logLevelDefault = logLevel["Default"]?.ToString() ?? logLevelDefault;
                    logLevelACTApi = logLevel["ACTApi"]?.ToString() ?? logLevelACTApi;
                }
            }
            catch
            {
                // JSON parse failed — keep defaults, app starts anyway
            }
        }
    }
}
