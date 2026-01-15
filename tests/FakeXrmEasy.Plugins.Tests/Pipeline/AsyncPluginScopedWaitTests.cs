using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Abstractions.Enums;
using FakeXrmEasy.Abstractions.Plugins.Enums;
using FakeXrmEasy.Middleware;
using FakeXrmEasy.Middleware.Crud;
using FakeXrmEasy.Middleware.Messages;
using FakeXrmEasy.Middleware.Pipeline;
using FakeXrmEasy.Plugins.PluginSteps;
using FakeXrmEasy.Extensions;
using FakeXrmEasy.Plugins;
using FakeXrmEasy.Plugins.Extensions;
using FakeXrmEasy.Pipeline;
using Microsoft.Xrm.Sdk;
using Xunit;
using DataverseEntities;

namespace FakeXrmEasy.Plugins.Tests.Pipeline
{
    public class Scoped_SlowAsyncPlugin : IPlugin
    {
        public static bool IsExecuted { get; set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            Thread.Sleep(500); // Simulate work blocking the task
            IsExecuted = true;
        }
    }

    public class Scoped_FastAsyncPlugin : IPlugin
    {
        public static bool IsExecuted { get; set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            Thread.Sleep(50); 
            IsExecuted = true;
        }
    }

    public class AsyncPluginScopedWaitTests
    {
        private readonly IXrmFakedContext _context;
        private readonly IOrganizationService _service;

        public AsyncPluginScopedWaitTests()
        {
            _context = MiddlewareBuilder
                        .New()
                        .AddCrud()
                        .AddFakeMessageExecutors()
                        .AddGenericFakeMessageExecutors()
                        .AddPipelineSimulation()
                        .UsePipelineSimulation()
                        .UseCrud()
                        .UseMessages()
                        .SetLicense(FakeXrmEasyLicense.NonCommercial)
                        .Build();

            _context.EnableProxyTypes(Assembly.GetAssembly(typeof(Account)));
            _service = _context.GetOrganizationService();
        }

        [Fact]
        public async System.Threading.Tasks.Task ExecuteWithTracking_Should_Wait_Only_For_Scoped_Plugins()
        {
            Scoped_SlowAsyncPlugin.IsExecuted = false;
            Scoped_FastAsyncPlugin.IsExecuted = false;

            var entityA = new Account { Id = Guid.NewGuid() };
            var entityB = new Contact { Id = Guid.NewGuid() };

            // Register Slow Plugin on Account (Async)
            // RegisterPluginStep<TPlugin, TEntity>(context, message, stage, ...)
            _context.RegisterPluginStep<Scoped_SlowAsyncPlugin, Account>(
                "Create",
                ProcessingStepStage.Postoperation,
                ProcessingStepMode.Asynchronous);

            // Register Fast Plugin on Contact (Async)
            _context.RegisterPluginStep<Scoped_FastAsyncPlugin, Contact>(
                "Create",
                ProcessingStepStage.Postoperation,
                ProcessingStepMode.Asynchronous);

            // 1. Trigger Slow Plugin (Global context)
            _service.Create(entityA);

            // 2. Trigger Fast Plugin with Scoped Tracking
            // Use context extension method
            var tracking = _context.CreateWithTracking(entityB);

            // 3. Wait for ONLY the fast plugin
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await tracking.WaitForPluginsAsync(TimeSpan.FromSeconds(2));
            sw.Stop();

            Assert.True(Scoped_FastAsyncPlugin.IsExecuted, "Fast plugin should be executed");
            Assert.False(Scoped_SlowAsyncPlugin.IsExecuted, "Slow plugin should NOT be executed yet (we didn't wait for it)");
            Assert.True(sw.ElapsedMilliseconds < 450, $"Wait took {sw.ElapsedMilliseconds}ms, likely waited for global context.");
            
            // Clean up
            await _context.WaitForAsyncPluginsAsync(TimeSpan.FromSeconds(5));
        }
    }
}
