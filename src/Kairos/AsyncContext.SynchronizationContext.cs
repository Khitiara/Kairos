using Microsoft.CodeAnalysis.PooledObjects;

namespace Kairos;

public sealed partial class AsyncContext
{
    private sealed class AsyncContextSynchronizationContext(AsyncContext context) : SynchronizationContext
    {
        internal readonly AsyncContext Context = context;

        public override SynchronizationContext CreateCopy() => new AsyncContextSynchronizationContext(Context);

        public override void Post(SendOrPostCallback d, object? state) {
            Context.Schedule(d, state);
        }

        public override void Send(SendOrPostCallback d, object? state) {
            if (AsyncContext.Current == Context) {
                d(state);
                return;
            }

            // cant fast path, pool a delegate to cut back on boxing and Post it
            // *should* be true that this path can't execute within AsyncContext.Execute
            // if it somehow does that's a deadlock and we're screwed
            using ManualResetEventSlim slim = new();
            QueuedWorkItem item = new(d, state);
            using (PooledDelegates.GetPooledSendOrPostCallback((_, t) => {
                       (QueuedWorkItem i, ManualResetEventSlim s) = t;
                       i.Invoke();
                       s.Set();
                   }, (item, slim), out SendOrPostCallback action)) {
                Post(action, null);
                slim.Wait();
            }
        }

        public override void OperationCompleted() {
            Context.OperationCompleted();
        }

        public override void OperationStarted() {
            Context.OperationStarted();
        }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) ||
                                                    obj is AsyncContextSynchronizationContext other &&
                                                    Context.Equals(other.Context);

        public override int GetHashCode() => Context.GetHashCode();
    }
}