using System;
using System.IO;
using Tekla.Structures.Model;

namespace RAM.Plugins.ColumnJointGP1.Services
{
    public enum LogLevel { Info, Warning, Error, Success }

    public static class Logger
    {
        private static readonly string _logFilePath;

        static Logger()
        {
            try
            {
                var model = new Model();
                if (model.GetConnectionStatus())
                {
                    // Лог будет писаться в папку модели под новым именем
                    _logFilePath = Path.Combine(model.GetInfo().ModelPath, "RAM_ColumnJointGP1.log");
                }
            }
            catch { }
        }

        public static void Write(string message, LogLevel level = LogLevel.Info)
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string levelTag = $"[{level.ToString().ToUpper()}]";
                File.AppendAllText(_logFilePath, $"{timestamp} {levelTag} {message}{Environment.NewLine}");
            }
            catch { }
        }
    }
}