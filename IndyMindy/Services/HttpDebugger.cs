using System;
using System.Diagnostics;
using System.Text;

namespace IndyMindy.Services
{
    public static class HttpDebugger
    {
        private static readonly object _lock = new object();
        private static bool _isEnabled = true;

        public static bool IsEnabled
        {
            get { lock (_lock) return _isEnabled; }
            set { lock (_lock) _isEnabled = value; }
        }

        public static void LogRequest(string method, string url, string headers, string body)
        {
            if (!IsEnabled) return;

            lock (_lock)
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== HTTP REQUEST ===");
                sb.AppendLine($"Method: {method}");
                sb.AppendLine($"URL: {url}");
                sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                
                if (!string.IsNullOrEmpty(headers))
                {
                    sb.AppendLine("Headers:");
                    sb.AppendLine(headers);
                }
                
                if (!string.IsNullOrEmpty(body))
                {
                    sb.AppendLine("Body:");
                    sb.AppendLine(body);
                }
                
                sb.AppendLine("==================");
                
                Debug.WriteLine(sb.ToString());
                Console.WriteLine(sb.ToString());
            }
        }

        public static void LogResponse(string method, string url, int statusCode, string headers, string body, TimeSpan duration)
        {
            if (!IsEnabled) return;

            lock (_lock)
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== HTTP RESPONSE ===");
                sb.AppendLine($"Method: {method}");
                sb.AppendLine($"URL: {url}");
                sb.AppendLine($"Status: {statusCode}");
                sb.AppendLine($"Duration: {duration.TotalMilliseconds:F2}ms");
                sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                
                if (!string.IsNullOrEmpty(headers))
                {
                    sb.AppendLine("Headers:");
                    sb.AppendLine(headers);
                }
                
                if (!string.IsNullOrEmpty(body))
                {
                    sb.AppendLine("Body:");
                    sb.AppendLine(body);
                }
                
                sb.AppendLine("===================");
                
                Debug.WriteLine(sb.ToString());
                Console.WriteLine(sb.ToString());
            }
        }

        public static void LogError(string method, string url, Exception exception, TimeSpan? duration = null)
        {
            if (!IsEnabled) return;

            lock (_lock)
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== HTTP ERROR ===");
                sb.AppendLine($"Method: {method}");
                sb.AppendLine($"URL: {url}");
                sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                
                if (duration.HasValue)
                {
                    sb.AppendLine($"Duration: {duration.Value.TotalMilliseconds:F2}ms");
                }
                
                sb.AppendLine($"Exception Type: {exception.GetType().Name}");
                sb.AppendLine($"Exception Message: {exception.Message}");
                sb.AppendLine($"Stack Trace: {exception.StackTrace}");
                
                sb.AppendLine("==================");
                
                Debug.WriteLine(sb.ToString());
                Console.WriteLine(sb.ToString());
            }
        }

        public static void LogInfo(string message)
        {
            if (!IsEnabled) return;

            lock (_lock)
            {
                var logMessage = $"[HTTP DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
                Debug.WriteLine(logMessage);
                Console.WriteLine(logMessage);
            }
        }

        public static void ClearLog()
        {
            Debug.WriteLine("=== HTTP DEBUG LOG CLEARED ===");
            Console.WriteLine("=== HTTP DEBUG LOG CLEARED ===");
        }
    }
} 