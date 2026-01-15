using System;
using System.Diagnostics;
using System.Threading;
using SystemTasks = System.Threading.Tasks;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Abstractions.Plugins.Enums;
using FakeXrmEasy.Plugins.PluginSteps;
using Xunit;
using FakeXrmEasy.Plugins.Extensions;
using FakeXrmEasy.Pipeline;
using Microsoft.Xrm.Sdk;
using DataverseEntities;

namespace FakeXrmEasy.Plugins.Tests.Pipeline
{
    public class SlowAsyncPlugin : IPlugin
    {
        public static bool IsExecuted = false;
        
        public void Execute(IServiceProvider serviceProvider)
        {
            Thread.Sleep(500);
            IsExecuted = true;
        }
    }

    public class AsyncPluginTests : FakeXrmEasyPipelineTestsBase
    {
        [Fact]
        public async SystemTasks.Task Create_Should_Execute_Async_Plugin_In_Background()
        {
            SlowAsyncPlugin.IsExecuted = false;

            _context.RegisterPluginStep<SlowAsyncPlugin, Account>("Create", ProcessingStepStage.Postoperation, ProcessingStepMode.Asynchronous);

            var account = new Account { Name = "Test" };
            
            var sw = Stopwatch.StartNew();
            _service.Create(account);
            sw.Stop();

            // Should return quickly (e.g. < 400ms) even if plugin takes 500ms
            
            // If it executed synchronously, IsExecuted would be true because the plugin sleeps for 500ms
            Assert.False(SlowAsyncPlugin.IsExecuted, "Plugin should not have finished yet");

            // Hypothetical API
            await _context.WaitForAsyncPluginsAsync(TimeSpan.FromSeconds(2));

            Assert.True(SlowAsyncPlugin.IsExecuted, "Plugin should have executed after awaiting");
        }
    }
}
