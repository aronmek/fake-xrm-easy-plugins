using System;
using System.Reflection;
using System.Linq;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Abstractions.Plugins;
using FakeXrmEasy.Middleware.Pipeline;
using FakeXrmEasy.Pipeline; // Added missing namespace
using FakeXrmEasy.Plugins.PluginSteps;
using FakeXrmEasy.Extensions;
using Xunit;
using Microsoft.Xrm.Sdk;
using FakeXrmEasy.Abstractions.Plugins.Enums; // Fix for ProcessingStepStage

namespace FakeXrmEasy.Plugins.Tests.Pipeline
{
    public class PipelineEventsTests : FakeXrmEasyPipelineTestsBase 
    {
        public class PreOperationPlugin : IPlugin
        {
            public void Execute(IServiceProvider serviceProvider)
            {
                var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                var target = (Entity)context.InputParameters["Target"];
                target["name"] = "Updated Name";
            }
        }

        [Fact]
        public void Should_trigger_pipeline_event_on_plugin_execution()
        {
            var wasCalled = false;
            
            // Setup pipeline simulation with events
            var events = _context.EnablePipelineEvents();
            
            events.OnPluginStepStart += (sender, args) => {
                wasCalled = true;
                Assert.EndsWith("PreOperationPlugin", args.PluginStep.PluginType.Split(',')[0].Trim());
            };

            // Register Plugin
            _context.RegisterPluginStep<PreOperationPlugin>(new PluginStepDefinition
            {
                EntityLogicalName = "account",
                MessageName = "Create",
                Stage = ProcessingStepStage.Preoperation
            });

            // Execute
            var service = _context.GetOrganizationService();
            service.Create(new Entity("account") { Id = Guid.NewGuid() });

            Assert.True(wasCalled, "Pipeline event should have been triggered");
        }
    }
}
