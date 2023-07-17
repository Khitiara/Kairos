using Microsoft.CodeAnalysis.PooledObjects;

namespace Kairos;

public class SetSynchronizationContextTaskScheduler : TaskScheduler, IDisposable
{
    private readonly SynchronizationContext _synchronizationContext;

    private readonly SendOrPostCallback       _callback;
    private readonly PooledDelegates.Releaser _releaser;

    public SetSynchronizationContextTaskScheduler(SynchronizationContext synchronizationContext) {
        _releaser = PooledDelegates.GetPooledSendOrPostCallback((o, scheduler) => {
            if (o is Task task)
                scheduler.TryExecuteTask(task);
        }, this, out _callback);
        _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
    }

    protected override IEnumerable<Task>? GetScheduledTasks() => null;

    protected override void QueueTask(Task task) {
        _synchronizationContext.Post(_callback, task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) =>
        SynchronizationContext.Current == _synchronizationContext && TryExecuteTask(task);

    public override int MaximumConcurrencyLevel => 1;
    public void Dispose() {
        _releaser.Dispose();
        GC.SuppressFinalize(this);
    }
}