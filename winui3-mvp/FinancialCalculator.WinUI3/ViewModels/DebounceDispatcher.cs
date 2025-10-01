using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinancialCalculator.WinUI3.ViewModels;

public sealed class DebounceDispatcher
{
    private CancellationTokenSource? _cts;

    public void Debounce(int millisecondsDelay, Func<CancellationToken, Task> action)
    {
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(millisecondsDelay, cts.Token);
                if (!cts.IsCancellationRequested)
                {
                    await action(cts.Token);
                }
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
        }, cts.Token);
    }
}
