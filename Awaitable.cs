using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

using CT = System.Threading.CancellationToken;

public static partial class AwaitableUtility {
    /// <returns>IsCancelled</returns>
    public static async Awaitable<bool> SuppressThrow(this Awaitable awaitable) {
        try {
            await awaitable;
            return false;
        } catch (OperationCanceledException) {
            return true;
        }
    }

    /// <returns>(IsCancelled,Result?)</returns>
    public static async Awaitable<(bool isCancelled, T? value)> SuppressThrow<T>(this Awaitable<T> awaitable) {
        try {
            var result = await awaitable;
            return (false, result);
        } catch (OperationCanceledException) {
            return (true, default);
        }
    }

    public static void Forget(this Awaitable awaitable) { }
    public static void Forget<T>(this Awaitable<T> awaitable) { }

    public static async Awaitable<UnityWebRequest> WithCancellation(this UnityWebRequest request, CT cancellationToken, Action<float>? progress = null) {
        var wr = request.SendWebRequest();
        while (!wr.isDone) {
            if (cancellationToken.IsCancellationRequested) {
                request.Abort();
                cancellationToken.ThrowIfCancellationRequested();
            }
            progress?.Invoke(wr.progress);
            await Awaitable.Yield();
        }
        return request;
    }

    public static async Awaitable<T[]> WithCancellation<T>(this AsyncInstantiateOperation<T> operation, CT cancellationToken) {
        while (!operation.isDone) {
            if (cancellationToken.IsCancellationRequested) {
                operation.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }
            await Awaitable.Yield();
        }
        return operation.Result;
    }

    public static async Awaitable<T> WithCancellation<T>(this T operation, CT cancellationToken, Action<float>? progress = null) where T : AsyncOperation {
        while (!operation.isDone) {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Invoke(operation.progress);
            await Awaitable.Yield();
        }
        return operation;
    }

    public static async Awaitable AsAwaitable(this IEnumerator coroutine, MonoBehaviour monoBehaviour, CT cancellationToken = default) {
        static IEnumerator wrapper(IEnumerator coroutine, Action onCompleted) {
            yield return coroutine;
            onCompleted.Invoke();
        }
        var completed = false;
        var routine = monoBehaviour.StartCoroutine(wrapper(coroutine, () => completed = true));
        await Awaitable.WaitUntilAsync(() => completed, cancellationToken);
        if (cancellationToken.IsCancellationRequested) {
            monoBehaviour.StopCoroutine(routine);
        }
    }
}

[AsyncMethodBuilder(typeof(Awaitable.AwaitableAsyncMethodBuilder<>))]
public partial struct Awaitable<T> {
    UnityEngine.Awaitable<T> wrapped;

    public UnityEngine.Awaitable<T>.Awaiter GetAwaiter() {
        return wrapped.GetAwaiter();
    }

    public void Cancel() {
        wrapped.Cancel();
    }

    public IEnumerator ToCoroutine(Action<T>? callback = null) {
        var awaiter = wrapped.GetAwaiter();
        yield return new WaitUntil(() => awaiter.IsCompleted);
        callback?.Invoke(awaiter.GetResult());
    }

    internal Awaitable(UnityEngine.Awaitable<T> wrapped) {
        this.wrapped = wrapped;
    }

}

[AsyncMethodBuilder(typeof(AwaitableAsyncMethodBuilder))]
public partial struct Awaitable: IEnumerator {
    public static Awaitable Yield(CT cancellationToken = default) {
        var wrap = UnityEngine.Awaitable.NextFrameAsync(cancellationToken);
        return new Awaitable(wrap);
    }
    public static Awaitable WaitForSecondsAsync(float seconds, CT cancellationToken = default) {
        var wrap = UnityEngine.Awaitable.WaitForSecondsAsync(seconds, cancellationToken);
        return new Awaitable(wrap);
    }

    public static Awaitable FromAsyncOperation(AsyncOperation op, CT cancellationToken = default) {
        var wrap = UnityEngine.Awaitable.FromAsyncOperation(op, cancellationToken);
        return new Awaitable(wrap);
    }
    public static Awaitable FixedUpdateAsync(CT cancellationToken = default) {
        var wrap = UnityEngine.Awaitable.FixedUpdateAsync(cancellationToken);
        return new Awaitable(wrap);
    }
    public static MainThreadAwaitable MainThreadAsync() {
        var wrap = UnityEngine.Awaitable.MainThreadAsync();
        return wrap;
    }
    public static UnityEngine.BackgroundThreadAwaitable BackgroundThreadAsync() {
        return UnityEngine.Awaitable.BackgroundThreadAsync();
    }
    public static Awaitable NextFrameAsync(CT cancellationToken = default) {
        var wrap = UnityEngine.Awaitable.NextFrameAsync(cancellationToken);
        return new Awaitable(wrap);
    }
    public static Awaitable EndOfFrameAsync(CT cancellationToken = default) {
        var wrap = UnityEngine.Awaitable.EndOfFrameAsync(cancellationToken);
        return new Awaitable(wrap);
    }
    public static async Awaitable WaitWhileAsync(Func<bool> condition, CT cancellationToken = default) {
        while (condition.Invoke()) await UnityEngine.Awaitable.FixedUpdateAsync(cancellationToken);
    }
    public static async Awaitable WaitUntilAsync(Func<bool> condition, CT cancellationToken = default) {
        while (!condition.Invoke()) {

            cancellationToken.ThrowIfCancellationRequested();
            await UnityEngine.Awaitable.FixedUpdateAsync(cancellationToken);
        }
    }
    public static void Void(Func<Awaitable> func) {
        _ = func.Invoke();
    }
    public static async Awaitable WhenAllAsync(IEnumerable<Awaitable> awaitables, CT cancellationToken = default) {
        var awaiters = awaitables.Select(x => x.GetAwaiter());
        var isAllCompleted = true;
        do {
            isAllCompleted = true;
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var awaiter in awaiters) {
                if (!awaiter.IsCompleted) {
                    isAllCompleted = false;
                    await Awaitable.Yield();
                    break;
                }
            }
        } while (!isAllCompleted);
    }
    public static async Awaitable<IEnumerable<T>> WhenAllAsync<T>(IEnumerable<Awaitable<T>> awaitables, CT cancellationToken = default) {
        var awaiters = awaitables.Select(x => x.GetAwaiter());
        var isAllCompleted = true;
        do {
            isAllCompleted = true;
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var awaiter in awaiters) {
                if (!awaiter.IsCompleted) {
                    isAllCompleted = false;
                    await Awaitable.Yield();
                    break;
                }
            }
        } while (!isAllCompleted);
        return awaiters.Select(x => x.GetResult());
    }
    public static async Awaitable WhenAnyAsync(IEnumerable<Awaitable> awaitables, CT cancellationToken = default) {
        var awaiters = awaitables.Select(x => x.GetAwaiter());

        var isAnyCompleted = false;
        do {
            isAnyCompleted = false;
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var awaiter in awaiters) {
                if (awaiter.IsCompleted) {
                    isAnyCompleted = true;
                    break;
                }
            }
            if (!isAnyCompleted) await Awaitable.Yield();
        } while (!isAnyCompleted);
    }
    public static async Awaitable<T> WhenAnyAsync<T>(IEnumerable<Awaitable<T>> awaitables, CT cancellationToken = default) {
        var awaiters = awaitables.Select(x => x.GetAwaiter());
        UnityEngine.Awaitable<T>.Awaiter? result = null;
        do {
            result = null;
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var awaiter in awaiters) {
                if (awaiter.IsCompleted) {
                    result = awaiter;
                    break;
                }
            }
            if (!result.HasValue) await Awaitable.Yield();
        } while (!result.HasValue);
        return result.Value.GetResult();
    }

    UnityEngine.Awaitable wrapped;

    public bool IsCompleted => wrapped.IsCompleted;
    object IEnumerator.Current => null!;

    public UnityEngine.Awaitable.Awaiter GetAwaiter() {
        return wrapped.GetAwaiter();
    }

    public void Cancel() {
        wrapped.Cancel();
    }

    Awaitable(UnityEngine.Awaitable wrapped) {
        this.wrapped = wrapped;
    }

    bool IEnumerator.MoveNext() {
        return ((IEnumerator)wrapped).MoveNext();
    }

    void IEnumerator.Reset() { }

    public struct AwaitableAsyncMethodBuilder {
        UnityEngine.Awaitable.AwaitableAsyncMethodBuilder wrapped;
        public static AwaitableAsyncMethodBuilder Create() {
            var builder = new AwaitableAsyncMethodBuilder {
                wrapped = default(UnityEngine.Awaitable.AwaitableAsyncMethodBuilder)
            };
            return builder;
        }
        public Awaitable Task => new Awaitable(wrapped.Task);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception e) {
            wrapped.SetException(e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult() {
            wrapped.SetResult();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {
            wrapped.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {
            wrapped.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
            wrapped.Start(ref stateMachine);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) {
            wrapped.SetStateMachine(stateMachine);
        }
    }

    public struct AwaitableAsyncMethodBuilder<T> {
        UnityEngine.Awaitable.AwaitableAsyncMethodBuilder<T> wrapped;
        public static AwaitableAsyncMethodBuilder<T> Create() {
            var builder = new AwaitableAsyncMethodBuilder<T> {
                wrapped = default(UnityEngine.Awaitable.AwaitableAsyncMethodBuilder<T>)
            };
            return builder;
        }
        public Awaitable<T> Task => new Awaitable<T>(wrapped.Task);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception e) {
            wrapped.SetException(e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T value) {
            wrapped.SetResult(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine {
            wrapped.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine {
            wrapped.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {
            wrapped.Start(ref stateMachine);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStateMachine(IAsyncStateMachine stateMachine) {
            wrapped.SetStateMachine(stateMachine);
        }
    }
}
