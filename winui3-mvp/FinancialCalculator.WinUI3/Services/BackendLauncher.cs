using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FinancialCalculator.WinUI3.Services
{
    public static class BackendLauncher
    {
        public static async Task<(Process? process, string baseUrl)> TryStartAsync(int port = 8123, CancellationToken ct = default)
        {
            string exePath = Path.Combine(AppContext.BaseDirectory, "fc-svc.exe");
            string baseUrl = $"http://127.0.0.1:{port}/";

            // If backend is already running and responsive, reuse
            if (await IsHealthyAsync(baseUrl, ct))
            {
                return (null, baseUrl);
            }

            if (!File.Exists(exePath))
            {
                // No embedded backend; assume external backend is used
                return (null, baseUrl);
            }

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            psi.Environment["FC_SVC_PORT"] = port.ToString();

            var proc = Process.Start(psi);
            if (proc != null)
            {
                // Give it a moment to bind, then probe health
                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 4000)
                {
                    if (ct.IsCancellationRequested) break;
                    if (await IsHealthyAsync(baseUrl, ct)) break;
                    await Task.Delay(200, ct);
                }
            }

            return (proc, baseUrl);
        }

        public static void TryStop(Process? proc)
        {
            try
            {
                if (proc == null) return;
                if (!proc.HasExited)
                {
                    proc.Kill(true);
                }
            }
            catch
            {
                // swallow
            }
        }

        private static async Task<bool> IsHealthyAsync(string baseUrl, CancellationToken ct)
        {
            try
            {
                using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMilliseconds(500) };
                var resp = await http.GetAsync("healthz", ct);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
