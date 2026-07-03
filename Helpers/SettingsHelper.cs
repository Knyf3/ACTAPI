using Newtonsoft.Json.Linq;

namespace ACTApi.Helpers
{
    public class SettingsHelper
    {
        public static JObject SettingsConfig { get; set; } = new();
        public static string FileSettings { get; set; } = "";
        public string ConnString { get; set; } = "";
        public string serverAddress { get; set; } = "http://localhost:8021";
        public string userName { get; set; } = "";
        public string password { get; set; } = "";
        public string appName { get; set; } = "ACT_API_Bridge";
        public string sharedSettingsPath { get; set; }
        public string actServer { get; set; } = "localhost:8004";

        public SettingsHelper()
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Try Settings\Settings.json first (production/deployment path)
            sharedSettingsPath = Path.GetFullPath(
                Path.Combine(exeDirectory, "Settings", "Settings.json"));

            if (!File.Exists(sharedSettingsPath))
            {
                // Fallback: project root (development path, running from VS)
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
                // No settings file found — use defaults, let the app start
                // The health endpoint will show the missing config
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
            }
            catch
            {
                // If JSON parsing fails, keep defaults — app still starts
            }
        }
    }
}
