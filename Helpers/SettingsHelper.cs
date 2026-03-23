using Newtonsoft.Json.Linq;

namespace ACTProAPI.Helpers
{
    public class SettingsHelper
    {
        public static JObject SettingsConfig { get; set; }
        public static string FileSettings { get; set; }
        public string ConnString { get; set; }
        public string serverAddress { get; set; }
        public string userName { get; set; }
        public string password { get; set; }
        public string appName { get; set; }
        public string  sharedSettingsPath { get; set; }
        public string actServer { get; set; }


        //Setting up to read json settings file and ensure that it will read on the application directory
        //Settings file in Settings folder
        public SettingsHelper()
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            sharedSettingsPath = Path.Combine(exeDirectory, @"Settings\Settings.json");
            sharedSettingsPath = Path.GetFullPath(sharedSettingsPath);


            SettingsConfig = JObject.Parse(System.IO.File.ReadAllText(sharedSettingsPath));
            serverAddress = SettingsConfig["Server"].ToString();
            actServer = SettingsConfig["ACTServer"].ToString();
            userName = SettingsConfig["ACTUsername"].ToString();
            password = SettingsConfig["ACTPassword"].ToString();
            appName = SettingsConfig["AppName"].ToString();
            
        }
       

    }
}
