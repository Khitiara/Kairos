using AutoFixture.Xunit2;
using FluentAssertions;
using Kairos;

namespace UnitTests;

public class GenericAsyncWaitQueueTests
{
    [Fact]
    public void NewQueueIsEmpty() {
        new AsyncWaitQueue<int>().IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void NonEmptyWithItemEnqueued() {
        AsyncWaitQueue<int> queue = new();
        queue.Enqueue();
        queue.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void DequeueDefaultCompletesTask() {
        AsyncWaitQueue<int> queue = new();
        Task t = queue.Enqueue();
        t.IsCompleted.Should().BeFalse();
        queue.Dequeue();
        t.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void DequeueDefaultOnlyCompletesOneTask() {
        AsyncWaitQueue<int> queue = new();
        Task t1 = queue.Enqueue();
        Task t2 = queue.Enqueue();
        t1.IsCompleted.Should().BeFalse();
        t2.IsCompleted.Should().BeFalse();
        queue.Dequeue();
        t1.IsCompleted.Should().BeTrue();
        t2.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void DequeueAllDefaultCompletesAllTasks() {
        AsyncWaitQueue<int> queue = new();
        Task t1 = queue.Enqueue();
        Task t2 = queue.Enqueue();
        t1.IsCompleted.Should().BeFalse();
        t2.IsCompleted.Should().BeFalse();
        queue.DequeueAll();
        t1.IsCompleted.Should().BeTrue();
        t2.IsCompleted.Should().BeTrue();
    }

    [Theory, AutoData,]
    public void DequeueValueCompletesTask(int value) {
        AsyncWaitQueue<int> queue = new();
        Task<int> t = queue.Enqueue();
        t.IsCompleted.Should().BeFalse();
        queue.Dequeue(value);
        t.IsCompleted.Should().BeTrue();
        t.Result.Should().Be(value);
    }

    [Theory, AutoData,]
    public void DequeueValueOnlyCompletesOneTask(int value) {
        AsyncWaitQueue<int> queue = new();
        Task<int> t1 = queue.Enqueue();
        Task<int> t2 = queue.Enqueue();
        t1.IsCompleted.Should().BeFalse();
        t2.IsCompleted.Should().BeFalse();
        queue.Dequeue(value);
        t1.IsCompleted.Should().BeTrue();
        t1.Result.Should().Be(value);
        t2.IsCompleted.Should().BeFalse();
    }

    [Theory, AutoData,]
    public void DequeueAllValueCompletesAllTasks(int value) {
        AsyncWaitQueue<int> queue = new();
        Task<int> t1 = queue.Enqueue();
        Task<int> t2 = queue.Enqueue();
        t1.IsCompleted.Should().BeFalse();
        t2.IsCompleted.Should().BeFalse();
        queue.DequeueAll(value);
        t1.IsCompleted.Should().BeTrue();
        t1.Result.Should().Be(value);
        t2.IsCompleted.Should().BeTrue();
        t2.Result.Should().Be(value);
    }

    [Fact]
    public void SuccessTryCancelCancelsTask() {
        AsyncWaitQueue<int> queue = new();
        Task t = queue.Enqueue();
        queue.TryCancel(t, new CancellationToken(true)).Should().BeTrue();
        t.IsCanceled.Should().BeTrue();
    }

    [Fact]
    public void SuccessTryCancelRemovesTask() {
        AsyncWaitQueue<int> queue = new();
        Task t = queue.Enqueue();
        queue.TryCancel(t, new CancellationToken(true)).Should().BeTrue();
        queue.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void FailedTryCancelDoesNotRemove() {
        AsyncWaitQueue<int> queue = new();
        Task t = queue.Enqueue();
        queue.Enqueue();
        queue.Dequeue();
        queue.TryCancel(t, new CancellationToken(true)).Should().BeFalse();
        queue.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ExtendedEnqueueCancelCancelsTask() {
        AsyncWaitQueue<int> queue = new();
        CancellationTokenSource cts = new();
        Task t = queue.Enqueue(new object(), cts.Token);
        await cts.CancelAsync();
        t.IsCanceled.Should().BeTrue();
    }

    [Fact]
    public async Task ExtendedEnqueueCancelRemovesTask() {
        AsyncWaitQueue<int> queue = new();
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
        AsyncWaitQueue<int> queue = new();
        queue.Enqueue(new object(), new CancellationToken(true)).IsCanceled.Should().BeTrue();
    }

    [Fact]
    public void CancelBeforeExtendedEnqueueRemovesSynchronously() {
        AsyncWaitQueue<int> queue = new();
        queue.Enqueue(new object(), new CancellationToken(true));
        queue.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void CancelAllClearsList() {
        AsyncWaitQueue<int> queue = new();
        queue.Enqueue();
        queue.Enqueue();
        queue.CancelAll(new CancellationToken(true));
        queue.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void CancelAllCancelsTasks() {
        AsyncWaitQueue<int> queue = new();
        Task t1 = queue.Enqueue();
        Task t2 = queue.Enqueue();
        queue.CancelAll(new CancellationToken(true));
        t1.IsCanceled.Should().BeTrue();
        t2.IsCanceled.Should().BeTrue();
    }
}