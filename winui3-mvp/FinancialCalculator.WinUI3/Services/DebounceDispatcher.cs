using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinancialCalculator.WinUI3.Services
{
    /// <summary>
    /// Debounces rapid successive calls to delay execution until a quiet period
    /// </summary>
    public sealed class DebounceDispatcher
    {
        private CancellationTokenSource? _cts;
        private readonly object _lock = new();

        /// <summary>
        /// Debounce an action with the specified delay
        /// </summary>
        public void Debounce(int millisecondsDelay, Action action)
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = new CancellationTokenSource();

                var token = _cts.Token;
                Task.Delay(millisecondsDelay, token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        try
                        {
                            action();
                        }
                        catch
                        {
                            // Swallow exceptions in debounced actions
                        }
                    }
                }, TaskScheduler.Default);
            }
        }

        /// <summary>
        /// Debounce an async function with the specified delay
        /// </summary>
        public void DebounceAsync(int millisecondsDelay, Func<Task> asyncAction)
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = new CancellationTokenSource();

                var token = _cts.Token;
                Task.Delay(millisecondsDelay, token).ContinueWith(async t =>
                {
                    if (!t.IsCanceled)
                    {
                        try
                        {
                            await asyncAction();
                        }
                        catch
                        {
                            // Swallow exceptions in debounced actions
                        }
                    }
                }, TaskScheduler.Default);
            }
        }
    }
}