using System;
using System.IO;

namespace ISO11820System.Utilities
{
    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "system.log");

        public static void Info(string msg)
        {
            Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {msg}");
        }

        public static void Warn(string msg)
        {
            Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARN] {msg}");
        }

        public static void Error(string msg)
        {
            Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {msg}");
        }

        private static void Write(string text)
        {
            File.AppendAllText(LogPath, text + Environment.NewLine);
        }
    }
}
