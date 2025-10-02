using System;
using System.IO;
using System.Text;

namespace FinancialCalculator.WinUI3.Services
{
    public static class Logger
    {
        private static readonly object _gate = new();
        private static bool _initialized;
        private static string _logDir = string.Empty;
        private static string _logFile = string.Empty;

        public static string LogDirectory => _logDir;
        public static string LogFile => _logFile;

        public static void Init(string prefix = "app")
        {
            if (_initialized) return;
            try
            {
                var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _logDir = Path.Combine(baseDir, "FinancialCalculator", "logs");
                Directory.CreateDirectory(_logDir);

                var ts = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                _logFile = Path.Combine(_logDir, $"{prefix}-{ts}.log");
                _initialized = true;

                Info($"Logger initialized. LogFile={_logFile}");
                Info($"OS={Environment.OSVersion}, 64Bit={Environment.Is64BitProcess}, DotNet={Environment.Version}");
                Info($"FC_API_BASE={Environment.GetEnvironmentVariable("FC_API_BASE") ?? "(null)"}");
            }
            catch
            {
                // Ignore logging init failures
            }
        }

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message, Exception? ex = null)
        {
            var details = ex == null ? message : $"{message}{Environment.NewLine}{ex}";
            Write("ERROR", details);
        }

        public static void Write(string level, string message)
        {
            try
            {
                if (!_initialized) Init();
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
                lock (_gate)
                {
                    File.AppendAllText(_logFile, line, Encoding.UTF8);
                }
                System.Diagnostics.Debug.WriteLine(line);
            }
            catch
            {
                // Ignore logging failures
            }
        }
    }
}