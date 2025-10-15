using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinancialCalculator.WinUI3.Services
{
    /// <summary>
    /// Debounces rapid successive calls to delay execution until a quiet period.
    /// Ensures debounced actions run on the UI thread when constructed on UI thread.
    /// </summary>
    public sealed class DebounceDispatcher
    {
        private CancellationTokenSource? _cts;
        private readonly object _lock = new();
        private readonly SynchronizationContext? _syncContext;

        public DebounceDispatcher()
        {
            _syncContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// Debounce an action with the specified delay.
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
                    if (t.IsCanceled) return;

                    void Invoke()
                    {
                        try { action(); }
                        catch { /* swallow */ }
                    }

                    if (_syncContext != null) _syncContext.Post(_ => Invoke(), null);
                    else Invoke();
                }, TaskScheduler.Default);
            }
        }

        /// <summary>
        /// Debounce an async function with the specified delay.
        /// </summary>
        public void DebounceAsync(int millisecondsDelay, Func<Task> asyncAction)
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = new CancellationTokenSource();

                var token = _cts.Token;
                Task.Delay(millisecondsDelay, token).ContinueWith(t =>
                {
                    if (t.IsCanceled) return;

                    async void InvokeAsync()
                    {
                        try { await asyncAction(); }
                        catch { /* swallow */ }
                    }

                    if (_syncContext != null) _syncContext.Post(_ => InvokeAsync(), null);
                    else _ = Task.Run(asyncAction);
                }, TaskScheduler.Default);
            }
        }
    }
}