using System;
using System.Threading;
using System.Threading.Tasks;

namespace DTech.InAppFlex
{
    internal sealed class CancellableCompletion<T>
    {
        private static readonly Action<object> CancelCallback = static state => ((CancellableCompletion<T>)state).Cancel();

        private readonly TaskCompletionSource<T> _completion;
        private readonly CancellationToken _token;

        private CancellationTokenRegistration _registration;

        public Task<T> Task => _completion.Task;

        public CancellableCompletion(CancellationToken token)
        {
            _completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _token = token;
            if (token.CanBeCanceled)
            {
                _registration = token.Register(CancelCallback, this);
            }
        }

        public bool TrySetResult(T result)
        {
            _registration.Dispose();
            return _completion.TrySetResult(result);
        }

        private void Cancel()
        {
            _registration.Dispose();
            _completion.TrySetCanceled(_token);
        }
    }
}
