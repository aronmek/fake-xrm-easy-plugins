using System;
using System.Threading.Tasks;

namespace FakeXrmEasy.Pipeline
{
    public interface IAsyncPluginExecution
    {
        Task WaitForPluginsAsync(TimeSpan timeout);
    }

    internal class AsyncPluginExecution : IAsyncPluginExecution
    {
        private readonly IAsyncPluginBackgroundTaskManager _manager;
        private readonly Guid _scopeId;

        public AsyncPluginExecution(IAsyncPluginBackgroundTaskManager manager, Guid scopeId)
        {
            _manager = manager;
            _scopeId = scopeId;
        }

        public Task WaitForPluginsAsync(TimeSpan timeout)
        {
            return _manager.WaitForScopeAsync(_scopeId, timeout);
        }
    }
}
