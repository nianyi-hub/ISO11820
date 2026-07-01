using Microsoft.Extensions.Configuration;

namespace ISO11820System.Utilities
{
    /// <summary>
    /// 配置管理器（读取appsettings.json）
    /// </summary>
    public class ConfigManager
    {
        private readonly IConfiguration _configuration;
        private readonly string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        public ConfigManager(string configFilePath = "appsettings.json")
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(_baseDirectory)
                .AddJsonFile(configFilePath, optional: false, reloadOnChange: true)
                .Build();
        }

        private string ResolvePath(string path)
        {
            return Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(_baseDirectory, path));
        }

        // ===== 数据库配置 =====
        public string DatabaseProvider => _configuration["Database:Provider"] ?? "Sqlite";
        public string SqlitePath => ResolvePath(_configuration["Database:SqlitePath"] ?? Path.Combine("Data", "ISO11820.db"));

        // ===== 硬件配置 =====
        public int ConstPower => int.Parse(_configuration["Hardware:ConstPower"] ?? "2048");
        public double PidTemperature => double.Parse(_configuration["Hardware:PidTemperature"] ?? "750");
        public string SensorProtocol => _configuration["Hardware:SensorProtocol"] ?? "ModbusRtu";

        // ===== 仿真配置 =====
        public bool EnableSimulation => bool.Parse(_configuration["Simulation:EnableSimulation"] ?? "true");
        public bool SimulateSensors => bool.Parse(_configuration["Simulation:SimulateSensors"] ?? "true");
        public bool SimulatePidController => bool.Parse(_configuration["Simulation:SimulatePidController"] ?? "true");
        public double InitialFurnaceTemp => double.Parse(_configuration["Simulation:InitialFurnaceTemp"] ?? "25");
        public double TargetFurnaceTemp => double.Parse(_configuration["Simulation:TargetFurnaceTemp"] ?? "750");
        public double HeatingRatePerSecond => double.Parse(_configuration["Simulation:HeatingRatePerSecond"] ?? "40");
        public double TempFluctuation => double.Parse(_configuration["Simulation:TempFluctuation"] ?? "0.5");
        public double StableThreshold => double.Parse(_configuration["Simulation:StableThreshold"] ?? "3");
        public bool SimulateFlame => bool.Parse(_configuration["Simulation:SimulateFlame"] ?? "false");
        public double SurfaceFollowRatio => double.Parse(_configuration["Simulation:SurfaceFollowRatio"] ?? "0.3");
        public double CenterFollowRatio => double.Parse(_configuration["Simulation:CenterFollowRatio"] ?? "0.25");
        public double SurfaceApproachRate => double.Parse(_configuration["Simulation:SurfaceApproachRate"] ?? "0.02");
        public double CenterApproachRate => double.Parse(_configuration["Simulation:CenterApproachRate"] ?? "0.01");

        // ===== 文件存储配置 =====
        public string BaseDirectory => ResolvePath(_configuration["FileStorage:BaseDirectory"] ?? "Data");
        public string TestDataDirectory => ResolvePath(_configuration["FileStorage:TestDataDirectory"] ?? Path.Combine("Data", "TestData"));

        // ===== 报告配置 =====
        public string ReportOutputDirectory => ResolvePath(_configuration["Report:OutputDirectory"] ?? Path.Combine("Data", "Reports"));
        public bool EnablePdfExport => bool.Parse(_configuration["Report:EnablePdfExport"] ?? "true");

        // ===== 日志配置 =====
        public string LogDirectory => _configuration["Logging:LogDirectory"] ?? "Logs";
        public string LogFileName => _configuration["Logging:LogFileName"] ?? "ISO11820_{Date}.log";
        public string LogMinimumLevel => _configuration["Logging:MinimumLevel"] ?? "Information";
    }
}
