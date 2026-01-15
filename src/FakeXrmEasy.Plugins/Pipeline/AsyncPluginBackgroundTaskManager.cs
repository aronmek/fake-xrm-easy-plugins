using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.Threading;

namespace FakeXrmEasy.Pipeline
{
    public interface IAsyncPluginBackgroundTaskManager
    {
        void AddTask(Task task);
        Task WaitForAllAsync(TimeSpan timeout);
        IAsyncPluginScope BeginScope();
        Task WaitForScopeAsync(Guid scopeId, TimeSpan timeout);
    }

    public interface IAsyncPluginScope: IDisposable
    {
        Guid Id { get; }
    }

    internal class AsyncPluginScope : IAsyncPluginScope
    {
        private Action _onDispose;
        public Guid Id { get; }

        public AsyncPluginScope(Guid id, Action onDispose)
        {
            Id = id;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke();
        }
    }

    public class AsyncPluginBackgroundTaskManager : IAsyncPluginBackgroundTaskManager
    {
        private readonly ConcurrentBag<Task> _tasks = new ConcurrentBag<Task>();
        private readonly ConcurrentDictionary<Guid, ConcurrentBag<Task>> _scopedTasks = new ConcurrentDictionary<Guid, ConcurrentBag<Task>>();

#if NET452
        private static readonly string _scopeKey = "AsyncPluginScope_" + Guid.NewGuid().ToString();
#else
        private static readonly AsyncLocal<Guid?> _currentScope = new AsyncLocal<Guid?>();
#endif

        public void AddTask(Task task)
        {
            _tasks.Add(task);
            
#if NET452
            var val = System.Runtime.Remoting.Messaging.CallContext.LogicalGetData(_scopeKey);
            if (val is Guid scopeId)
            {
                 var bag = _scopedTasks.GetOrAdd(scopeId, new ConcurrentBag<Task>());
                 bag.Add(task);
            }
#else
            if (_currentScope.Value.HasValue)
            {
                var scopeId = _currentScope.Value.Value;
                // Ensure the bag exists
                var bag = _scopedTasks.GetOrAdd(scopeId, new ConcurrentBag<Task>());
                bag.Add(task);
            }
#endif
        }

        public IAsyncPluginScope BeginScope()
        {
            var scopeId = Guid.NewGuid();
#if NET452
            System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(_scopeKey, scopeId);
            return new AsyncPluginScope(scopeId, () => System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(_scopeKey, null));
#else
            _currentScope.Value = scopeId;
            return new AsyncPluginScope(scopeId, () => _currentScope.Value = null);
#endif
        }

        public async Task WaitForScopeAsync(Guid scopeId, TimeSpan timeout)
        {
            if (!_scopedTasks.TryGetValue(scopeId, out var tasksBag))
            {
                return; // Nothing to wait for
            }

            var tasksToWait = tasksBag.ToArray();
            if (tasksToWait.Length == 0) return;
            
            var allTasks = Task.WhenAll(tasksToWait);
            var delayTask = Task.Delay(timeout);

            var completedTask = await Task.WhenAny(allTasks, delayTask);
            if (completedTask == delayTask)
            {
                throw new TimeoutException($"Timed out waiting for {tasksToWait.Length} async plugin tasks to complete in scope {scopeId}.");
            }

            await allTasks;
        }

        public async Task WaitForAllAsync(TimeSpan timeout)
        {
            var tasksToWait = _tasks.ToArray();
            if (tasksToWait.Length == 0) return;

            var allTasks = Task.WhenAll(tasksToWait);
            var delayTask = Task.Delay(timeout);

            var completedTask = await Task.WhenAny(allTasks, delayTask);
            if (completedTask == delayTask)
            {
                throw new TimeoutException($"Timed out waiting for {tasksToWait.Length} async plugin tasks to complete.");
            }
            
            // Allow exceptions to propagate?
            await allTasks; 
        }
    }
}
