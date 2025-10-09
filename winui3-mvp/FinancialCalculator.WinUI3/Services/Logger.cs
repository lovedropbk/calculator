using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FinancialCalculator.WinUI3.Services
{
    public static class Logger
    {
        private static readonly object _gate = new();
        private static bool _initialized;
        private static string _logDir = string.Empty;
        private static string _logFile = string.Empty;
        private static readonly Queue<string> _recentLogs = new();
        private const int MaxRecentLogs = 1000;

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
                
                // Clean up old log files (keep last 10)
                CleanupOldLogs();
            }
            catch
            {
                // Ignore logging init failures
            }
        }

        private static void CleanupOldLogs()
        {
            try
            {
                var files = Directory.GetFiles(_logDir, "*.log");
                if (files.Length > 10)
                {
                    Array.Sort(files);
                    for (int i = 0; i < files.Length - 10; i++)
                    {
                        File.Delete(files[i]);
                    }
                }
            }
            catch { }
        }

        public static void Debug(string message, [CallerMemberName] string? memberName = null)
            => Write("DEBUG", message, memberName);

        public static void Info(string message, [CallerMemberName] string? memberName = null)
            => Write("INFO", message, memberName);

        public static void Warn(string message, [CallerMemberName] string? memberName = null)
            => Write("WARN", message, memberName);

        public static void Error(string message, Exception? ex = null, [CallerMemberName] string? memberName = null)
        {
            var details = ex == null ? message : $"{message}{Environment.NewLine}Exception Type: {ex.GetType().FullName}{Environment.NewLine}Message: {ex.Message}{Environment.NewLine}StackTrace:{Environment.NewLine}{ex.StackTrace}";
            if (ex?.InnerException != null)
            {
                details += $"{Environment.NewLine}Inner Exception: {ex.InnerException.GetType().FullName}{Environment.NewLine}Inner Message: {ex.InnerException.Message}{Environment.NewLine}Inner StackTrace:{Environment.NewLine}{ex.InnerException.StackTrace}";
            }
            Write("ERROR", details, memberName);
        }

        public static void ApiRequest(string method, string url, object? requestBody = null)
        {
            var message = $"API Request: {method} {url}";
            if (requestBody != null)
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(requestBody, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    message += $"{Environment.NewLine}Body: {json}";
                }
                catch
                {
                    message += $"{Environment.NewLine}Body: [Failed to serialize]";
                }
            }
            Write("API", message);
        }

        public static void ApiResponse(string method, string url, int statusCode, string? responseBody = null)
        {
            var message = $"API Response: {method} {url} - Status: {statusCode}";
            if (!string.IsNullOrEmpty(responseBody))
            {
                // Truncate large responses
                var body = responseBody.Length > 5000 ? responseBody.Substring(0, 5000) + "... [truncated]" : responseBody;
                message += $"{Environment.NewLine}Body: {body}";
            }
            Write("API", message);
        }

        public static void ApiError(string method, string url, Exception ex)
        {
            Error($"API Error: {method} {url}", ex);
        }

        public static void Write(string level, string message, string? caller = null)
        {
            try
            {
                if (!_initialized) Init();
                
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var callerInfo = string.IsNullOrEmpty(caller) ? "" : $" [{caller}]";
                var line = $"{timestamp} [{level}]{callerInfo} {message}{Environment.NewLine}";
                
                lock (_gate)
                {
                    // Add to recent logs queue
                    _recentLogs.Enqueue(line);
                    while (_recentLogs.Count > MaxRecentLogs)
                    {
                        _recentLogs.Dequeue();
                    }
                    
                    // Write to file
                    File.AppendAllText(_logFile, line, Encoding.UTF8);
                }
                
                System.Diagnostics.Debug.WriteLine(line);
            }
            catch
            {
                // Ignore logging failures
            }
        }

        public static string[] GetRecentLogs(int count = 100)
        {
            lock (_gate)
            {
                var logs = _recentLogs.ToArray();
                var startIndex = Math.Max(0, logs.Length - count);
                var result = new string[Math.Min(count, logs.Length)];
                Array.Copy(logs, startIndex, result, 0, result.Length);
                return result;
            }
        }

        public static async Task<string> GetLogContentAsync()
        {
            try
            {
                if (!_initialized || !File.Exists(_logFile))
                    return "No log file available";
                
                return await File.ReadAllTextAsync(_logFile, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return $"Error reading log file: {ex.Message}";
            }
        }

        public static void LogUnhandledException(Exception ex)
        {
            try
            {
                var message = $"UNHANDLED EXCEPTION{Environment.NewLine}" +
                             $"Type: {ex.GetType().FullName}{Environment.NewLine}" +
                             $"Message: {ex.Message}{Environment.NewLine}" +
                             $"Source: {ex.Source}{Environment.NewLine}" +
                             $"TargetSite: {ex.TargetSite}{Environment.NewLine}" +
                             $"StackTrace:{Environment.NewLine}{ex.StackTrace}";
                
                if (ex.InnerException != null)
                {
                    message += $"{Environment.NewLine}{Environment.NewLine}INNER EXCEPTION{Environment.NewLine}" +
                              $"Type: {ex.InnerException.GetType().FullName}{Environment.NewLine}" +
                              $"Message: {ex.InnerException.Message}{Environment.NewLine}" +
                              $"StackTrace:{Environment.NewLine}{ex.InnerException.StackTrace}";
                }
                
                Write("FATAL", message);
                
                // Also write to a separate crash log
                try
                {
                    var crashFile = Path.Combine(_logDir, $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                    File.WriteAllText(crashFile, message, Encoding.UTF8);
                }
                catch { }
            }
            catch { }
        }
    }
}