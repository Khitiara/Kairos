using System.Collections.Concurrent;

namespace Kairos;

public sealed partial class AsyncContext : IDisposable
{
    private readonly struct QueuedWorkItem(SendOrPostCallback callback, object? state)
    {
        internal readonly SendOrPostCallback Callback = callback;
        internal readonly object?            State    = state;

        internal void Invoke() => Callback(State);
    }

    private readonly BlockingCollection<QueuedWorkItem> _queuedWork;
    private readonly AsyncContextSynchronizationContext _synchronizationContext;

    public static AsyncContext? Current =>
        (SynchronizationContext.Current as AsyncContextSynchronizationContext)?.Context;

    private          int                                    _outstandingOps;
    private readonly SetSynchronizationContextTaskScheduler _taskScheduler;

    public AsyncContext() {
        _queuedWork = new BlockingCollection<QueuedWorkItem>();
        _synchronizationContext = new AsyncContextSynchronizationContext(this);
        _taskScheduler = new SetSynchronizationContextTaskScheduler(_synchronizationContext);
        Factory = new TaskFactory(CancellationToken.None, TaskCreationOptions.HideScheduler,
            TaskContinuationOptions.HideScheduler, _taskScheduler);
    }

    public int Id => _taskScheduler.Id;
    public TaskFactory Factory { get; }
    public TaskScheduler Scheduler => _taskScheduler;
    public SynchronizationContext SynchronizationContext => _synchronizationContext;

    private void OperationStarted() {
        Interlocked.Increment(ref _outstandingOps);
    }

    private void OperationCompleted() {
        if (Interlocked.Decrement(ref _outstandingOps) is 0)
            _queuedWork.CompleteAdding();
    }

    public void Schedule(SendOrPostCallback sendOrPostCallback, object? state) {
        if (!_queuedWork.IsAddingCompleted && _queuedWork.TryAdd(new QueuedWorkItem(sendOrPostCallback, state)))
            // call this here if adding succeeded - Execute will decrement after each delegate invoke
            // hopefully this negates most of the edge-cases from using OperationStarted and OperationCompleted
            OperationStarted();
    }

    public void Schedule(Action action) {
        Schedule(static s => ((Action)s!).Invoke(), action);
    }

    public void Dispose() {
        _queuedWork.Dispose();
        _taskScheduler.Dispose();
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void Execute() {
        SynchronizationContext? captured = SynchronizationContext.Current;
        try {
            SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
            foreach (QueuedWorkItem workItem in _queuedWork.GetConsumingEnumerable()) {
                workItem.Invoke();
                OperationCompleted();
            }
        }
        finally {
            SynchronizationContext.SetSynchronizationContext(captured);
        }
    }
}