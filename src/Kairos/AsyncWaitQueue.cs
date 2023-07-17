using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Kairos;

/// <summary>
/// An async wait queue. Enqueuing returns a Task which can be completed by calling Dequeue.
/// The implementation of this type assumes the caller holds a lock.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class AsyncWaitQueue
{
    private readonly LinkedList<TaskCompletionSource> _queue = new();

    public int Count => _queue.Count;

    public bool IsEmpty => Count is 0;

    public Task Enqueue() {
        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.AddLast(tcs);
        return tcs.Task;
    }

    public void Dequeue() {
        if (_queue.First is { Value: { } tcs, } node) {
            tcs.TrySetResult();
            _queue.Remove(node);
        }
    }

    public void DequeueAll() {
        foreach (TaskCompletionSource source in _queue)
            source.TrySetResult();
        _queue.Clear();
    }

    public bool TryCancel(Task task, CancellationToken cancellationToken) {
        for (LinkedListNode<TaskCompletionSource>? node = _queue.First;
             node is { Next: var next, Value: { } tcs, };
             node = next) {
            if (tcs.Task == task) {
                tcs.TrySetCanceled(cancellationToken);
                _queue.Remove(node);
                return true;
            }
        }

        return false;
    }

    public void CancelAll(CancellationToken cancellationToken) {
        foreach (TaskCompletionSource source in _queue)
            source.TrySetCanceled(cancellationToken);
        _queue.Clear();
    }

    [DebuggerNonUserCode, ExcludeFromCodeCoverage,]
    internal sealed class DebugView(AsyncWaitQueue queue)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Task[] Tasks {
            get {
                List<Task> result = new(queue.Count);
                result.AddRange(queue._queue.Select(entry => entry.Task));
                return result.ToArray();
            }
        }
    }
}