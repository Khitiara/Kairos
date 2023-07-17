namespace Kairos;

public static class AsyncWaitQueueExtensions
{
    /// <inheritdoc cref="Enqueue"/>
    public static Task<T> Enqueue<T>(this AsyncWaitQueue<T> self, object mutex, CancellationToken token) {
        if (token.IsCancellationRequested)
            return Task.FromCanceled<T>(token);

        Task<T> ret = self.Enqueue();
        if (!token.CanBeCanceled)
            return ret;

        // got this down to two boxing allocs thanks to state-passing overloads
        CancellationTokenRegistration registration = token.Register(static (state, tok) => {
            if (state is not (AsyncWaitQueue s, Task t, { } m))
                throw new InvalidOperationException();
            lock (m) s.TryCancel(t, tok);
        }, (self, ret, mutex));
        ret.ContinueWith((_, r) => ((CancellationTokenRegistration)r!).Dispose(), registration,
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return ret;
    }

    /// <summary>
    /// Creates a new entry and queues it to this wait queue. If the cancellation token is already canceled, this
    /// method immediately returns a canceled task without modifying the wait queue.
    /// </summary>
    /// <param name="self">The wait queue.</param>
    /// <param name="mutex">A synchronization object taken while cancelling the entry.</param>
    /// <param name="token">The token used to cancel the wait.</param>
    /// <returns>The queued task.</returns>
    public static Task Enqueue(this AsyncWaitQueue self, object mutex, CancellationToken token) {
        if (token.IsCancellationRequested)
            return Task.FromCanceled(token);

        Task ret = self.Enqueue();
        if (!token.CanBeCanceled)
            return ret;

        // got this down to two boxing allocs thanks to state-passing overloads
        CancellationTokenRegistration registration = token.Register(static (state, tok) => {
            if (state is not (AsyncWaitQueue s, Task t, { } m))
                throw new InvalidOperationException();
            lock (m) s.TryCancel(t, tok);
        }, (self, ret, mutex));
        ret.ContinueWith((_, r) => ((CancellationTokenRegistration)r!).Dispose(), registration,
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return ret;
    }
}