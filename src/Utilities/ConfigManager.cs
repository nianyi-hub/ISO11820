using System.IO;
using Newtonsoft.Json;

namespace ISO11820System.Utilities
{
    public class ConfigManager
    {
        private static AppConfig _instance;

        public static AppConfig LoadConfig()
        {
            if (_instance != null) return _instance;

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            string json = File.ReadAllText(path);
            _instance = JsonConvert.DeserializeObject<AppConfig>(json);
            return _instance;
        }
    }

    public class AppConfig
    {
        public string SqlitePath { get; set; }
        public double TargetFurnaceTemp { get; set; }
        public double HeatingRatePerSecond { get; set; }
        public double TempFluctuation { get; set; }
        public double StableThreshold { get; set; }
        public string ReportOutputDirectory { get; set; }
    }
}
