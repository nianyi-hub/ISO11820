using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;

namespace ISO11820System.Utilities
{
    /// <summary>
    /// 日志工具类（封装Serilog）
    /// </summary>
    public static class Logger
    {
        private static ILogger? _logger;

        /// <summary>
        /// 初始化日志系统
        /// </summary>
        public static void Initialize(string logDirectory = "Logs", string fileNameTemplate = "ISO11820_{Date}.log")
        {
            var logPath = Path.Combine(logDirectory, fileNameTemplate);

            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Logger = _logger;
        }

        public static void Info(string message) => _logger?.Information(message);
        public static void Warning(string message) => _logger?.Warning(message);
        public static void Error(string message, Exception? ex = null) => _logger?.Error(ex, message);
        public static void Debug(string message) => _logger?.Debug(message);
    }
}
