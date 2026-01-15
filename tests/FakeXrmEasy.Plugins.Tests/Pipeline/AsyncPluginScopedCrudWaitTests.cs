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
    public class Scoped_SlowUpdateAsyncPlugin : IPlugin
    {
        public static bool IsExecuted { get; set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            Thread.Sleep(500); 
            IsExecuted = true;
        }
    }

    public class Scoped_FastUpdateAsyncPlugin : IPlugin
    {
        public static bool IsExecuted { get; set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            Thread.Sleep(50); 
            IsExecuted = true;
        }
    }

    public class AsyncPluginScopedCrudWaitTests
    {
        private readonly IXrmFakedContext _context;
        private readonly IOrganizationService _service;

        public AsyncPluginScopedCrudWaitTests()
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
        public async System.Threading.Tasks.Task UpdateWithTracking_Should_Wait_Only_For_Scoped_Plugins()
        {
            Scoped_SlowUpdateAsyncPlugin.IsExecuted = false;
            Scoped_FastUpdateAsyncPlugin.IsExecuted = false;

            var entityA = new Account { Id = Guid.NewGuid() };
            var entityB = new Contact { Id = Guid.NewGuid() };

            _service.Create(entityA);
            _service.Create(entityB);

            // Register Slow Plugin on Account Update (Async)
            _context.RegisterPluginStep<Scoped_SlowUpdateAsyncPlugin, Account>(
                "Update",
                ProcessingStepStage.Postoperation,
                ProcessingStepMode.Asynchronous);

            // Register Fast Plugin on Contact Update (Async)
            _context.RegisterPluginStep<Scoped_FastUpdateAsyncPlugin, Contact>(
                "Update",
                ProcessingStepStage.Postoperation,
                ProcessingStepMode.Asynchronous);

            // 1. Trigger Slow Plugin (Global context)
            // We use standard Update, which will trigger the slow plugin on background
            var updateA = new Account { Id = entityA.Id, Name = "Updated Name" };
            _service.Update(updateA);

            // 2. Trigger Fast Plugin with Scoped Tracking
            var updateB = new Contact { Id = entityB.Id, FirstName = "Updated Name" };
            var tracking = _context.UpdateWithTracking(updateB);

            // 3. Wait for ONLY the fast plugin
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await tracking.WaitForPluginsAsync(TimeSpan.FromSeconds(2));
            sw.Stop();

            Assert.True(Scoped_FastUpdateAsyncPlugin.IsExecuted, "Fast plugin should be executed");
            Assert.False(Scoped_SlowUpdateAsyncPlugin.IsExecuted, "Slow plugin should NOT be executed yet");
            Assert.True(sw.ElapsedMilliseconds < 450, $"Wait took {sw.ElapsedMilliseconds}ms, likely waited for global context.");
            
            // Clean up
            await _context.WaitForAsyncPluginsAsync(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async System.Threading.Tasks.Task DeleteWithTracking_Should_Wait_Only_For_Scoped_Plugins()
        {
            Scoped_SlowUpdateAsyncPlugin.IsExecuted = false; // Reusing plugin classes for Delete test brevity
            Scoped_FastUpdateAsyncPlugin.IsExecuted = false;

            var entityA = new Account { Id = Guid.NewGuid() };
            var entityB = new Contact { Id = Guid.NewGuid() };

            _service.Create(entityA);
            _service.Create(entityB);

            // Register Slow Plugin on Account Delete (Async)
            _context.RegisterPluginStep<Scoped_SlowUpdateAsyncPlugin, Account>(
                "Delete",
                ProcessingStepStage.Postoperation,
                ProcessingStepMode.Asynchronous);

            // Register Fast Plugin on Contact Delete (Async)
            _context.RegisterPluginStep<Scoped_FastUpdateAsyncPlugin, Contact>(
                "Delete",
                ProcessingStepStage.Postoperation,
                ProcessingStepMode.Asynchronous);

            // 1. Trigger Slow Plugin (Global context)
            _service.Delete(Account.EntityLogicalName, entityA.Id);

            // 2. Trigger Fast Plugin with Scoped Tracking
            var tracking = _context.DeleteWithTracking(Contact.EntityLogicalName, entityB.Id);

            // 3. Wait for ONLY the fast plugin
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await tracking.WaitForPluginsAsync(TimeSpan.FromSeconds(2));
            sw.Stop();

            Assert.True(Scoped_FastUpdateAsyncPlugin.IsExecuted, "Fast plugin should be executed");
            Assert.False(Scoped_SlowUpdateAsyncPlugin.IsExecuted, "Slow plugin should NOT be executed yet");
            Assert.True(sw.ElapsedMilliseconds < 450, $"Wait took {sw.ElapsedMilliseconds}ms, likely waited for global context.");
            
            // Clean up
            await _context.WaitForAsyncPluginsAsync(TimeSpan.FromSeconds(5));
        }
    }
}
