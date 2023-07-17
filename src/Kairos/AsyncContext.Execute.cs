using Microsoft.CodeAnalysis.PooledObjects;

namespace Kairos;

public sealed partial class AsyncContext
{
    public static void Run(Action action) {
        ArgumentNullException.ThrowIfNull(action);
        using AsyncContext ctx = new();
        ctx.Schedule(action);
        ctx.Execute();
    }

    public static TResult Run<TResult>(Func<TResult> func) {
        ArgumentNullException.ThrowIfNull(func);
        using AsyncContext ctx = new();
        Task<TResult> task = ctx.Factory.StartNew(func);
        ctx.Execute();
        return task.GetAwaiter().GetResult();
    }

    public static void Run(Func<Task> func) {
        ArgumentNullException.ThrowIfNull(func);
        using AsyncContext ctx = new();
        Task task = ctx.Factory.StartNew(func).Unwrap();
        ctx.Execute();
        task.GetAwaiter().GetResult();
    }

    public static void Run(Func<ValueTask> func) {
        ArgumentNullException.ThrowIfNull(func);
        using AsyncContext ctx = new();
        using (PooledDelegates.GetPooledFunction(a => a().AsTask(), func, out var f)) {
            Task task = ctx.Factory.StartNew(f);
            ctx.Execute();
            task.GetAwaiter().GetResult();
        }
    }

    public static TResult Run<TResult>(Func<Task<TResult>> func) {
        ArgumentNullException.ThrowIfNull(func);
        using AsyncContext ctx = new();
        Task<TResult> task = ctx.Factory.StartNew(func).Unwrap();
        ctx.Execute();
        return task.GetAwaiter().GetResult();
    }

    public static TResult Run<TResult>(Func<ValueTask<TResult>> func) {
        ArgumentNullException.ThrowIfNull(func);
        using AsyncContext ctx = new();
        using (PooledDelegates.GetPooledFunction(a => a().AsTask(), func, out var f)) {
            Task<TResult> task = ctx.Factory.StartNew(f).Unwrap();
            ctx.Execute();
            return task.GetAwaiter().GetResult();
        }
    }
}