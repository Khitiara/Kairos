using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Kairos;

/// <summary>
/// An async wait queue. Enqueuing returns a Task which can be completed by passing an item to Dequeue
/// The implementation of this type assumes the caller holds a lock.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(AsyncWaitQueue<>.DebugView))]
public sealed class AsyncWaitQueue<T>
{
    private readonly LinkedList<TaskCompletionSource<T>> _queue = new();

    public int Count => _queue.Count;

    public bool IsEmpty => Count is 0;

    public Task<T> Enqueue() {
        TaskCompletionSource<T> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.AddLast(tcs);
        return tcs.Task;
    }

    public void Dequeue(T? result = default) {
        if (_queue.First is { Value: { } tcs, } node) {
            tcs.TrySetResult(result!);
            _queue.Remove(node);
        }
    }

    public void DequeueAll(T? result = default) {
        foreach (TaskCompletionSource<T> source in _queue)
            source.TrySetResult(result!);
        _queue.Clear();
    }

    public bool TryCancel(Task task, CancellationToken cancellationToken) {
        for (LinkedListNode<TaskCompletionSource<T>>? node = _queue.First;
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
        foreach (TaskCompletionSource<T> source in _queue)
            source.TrySetCanceled(cancellationToken);
        _queue.Clear();
    }

    [DebuggerNonUserCode, ExcludeFromCodeCoverage,]
    internal sealed class DebugView(AsyncWaitQueue<T> queue)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Task<T>[] Tasks {
            get {
                List<Task<T>> result = new(queue._queue.Count);
                result.AddRange(queue._queue.Select(entry => entry.Task));
                return result.ToArray();
            }
        }
    }
}