using FluentAssertions;
using Kairos;

namespace UnitTests;

public class VoidAsyncWaitQueueTests
{
    [Fact]
    public void NewQueueIsEmpty() {
        new AsyncWaitQueue().IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void NonEmptyWithItemEnqueued() {
        AsyncWaitQueue queue = new();
        queue.Enqueue();
        queue.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void DequeueCompletesTask() {
        AsyncWaitQueue queue = new();
        Task t = queue.Enqueue();
        t.IsCompleted.Should().BeFalse();
        queue.Dequeue();
        t.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void DequeueOnlyCompletesOneTask() {
        AsyncWaitQueue queue = new();
        Task t1 = queue.Enqueue();
        Task t2 = queue.Enqueue();
        t1.IsCompleted.Should().BeFalse();
        t2.IsCompleted.Should().BeFalse();
        queue.Dequeue();
        t1.IsCompleted.Should().BeTrue();
        t2.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void DequeueAllCompletesAllTasks() {
        AsyncWaitQueue queue = new();
        Task t1 = queue.Enqueue();
        Task t2 = queue.Enqueue();
        t1.IsCompleted.Should().BeFalse();
        t2.IsCompleted.Should().BeFalse();
        queue.DequeueAll();
        t1.IsCompleted.Should().BeTrue();
        t2.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void SuccessTryCancelCancelsTask() {
        AsyncWaitQueue queue = new();
        Task t = queue.Enqueue();
        queue.TryCancel(t, new CancellationToken(true)).Should().BeTrue();
        t.IsCanceled.Should().BeTrue();
    }

    [Fact]
    public void SuccessTryCancelRemovesTask() {
        AsyncWaitQueue queue = new();
        Task t = queue.Enqueue();
        queue.TryCancel(t, new CancellationToken(true)).Should().BeTrue();
        queue.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void FailedTryCancelDoesNotRemove() {
        AsyncWaitQueue queue = new();
        Task t = queue.Enqueue();
        queue.Enqueue();
        queue.Dequeue();
        queue.TryCancel(t, new CancellationToken(true)).Should().BeFalse();
        queue.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ExtendedEnqueueCancelCancelsTask() {
        AsyncWaitQueue queue = new();
        CancellationTokenSource cts = new();
        Task t = queue.Enqueue(new object(), cts.Token);
        await cts.CancelAsync();
        t.IsCanceled.Should().BeTrue();
    }

    [Fact]
    public async Task ExtendedEnqueueCancelRemovesTask() {
        AsyncWaitQueue queue = new();
        CancellationTokenSource cts = new();
        Task t = queue.Enqueue(new object(), cts.Token);
        await cts.CancelAsync();
        try {
            await t;
        }
        catch (OperationCanceledException) { }

        queue.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void CancelBeforeExtendedEnqueueCancelsSynchronously() {
        AsyncWaitQueue queue = new();
        queue.Enqueue(new object(), new CancellationToken(true)).IsCanceled.Should().BeTrue();
    }

    [Fact]
    public void CancelBeforeExtendedEnqueueRemovesSynchronously() {
        AsyncWaitQueue queue = new();
        queue.Enqueue(new object(), new CancellationToken(true));
        queue.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void CancelAllClearsList() {
        AsyncWaitQueue queue = new();
        queue.Enqueue();
        queue.Enqueue();
        queue.CancelAll(new CancellationToken(true));
        queue.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void CancelAllCancelsTasks() {
        AsyncWaitQueue queue = new();
        Task t1 = queue.Enqueue();
        Task t2 = queue.Enqueue();
        queue.CancelAll(new CancellationToken(true));
        t1.IsCanceled.Should().BeTrue();
        t2.IsCanceled.Should().BeTrue();
    }
}