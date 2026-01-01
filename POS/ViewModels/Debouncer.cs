using System;
using System.Threading;
using System.Threading.Tasks;

namespace POS.ViewModels
{
    public sealed class Debouncer : IDisposable
    {
        private CancellationTokenSource? _cts;

        public async Task DebounceAsync(Func<CancellationToken, Task> action, int delayMilliseconds)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            var token = _cts.Token;
            try
            {
                await Task.Delay(delayMilliseconds, token);
                if (!token.IsCancellationRequested)
                {
                    await action(token);
                }
            }
            catch (OperationCanceledException)
            {
                // Swallow cancellations to keep UI responsive.
            }
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
