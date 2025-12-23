using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FakeItEasy;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Abstractions.Plugins.Enums;
using FakeXrmEasy.Pipeline;
using FakeXrmEasy.Plugins.Definitions;
using FakeXrmEasy.Plugins.Pipeline;
using FakeXrmEasy.Plugins.PluginSteps;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace FakeXrmEasy.Plugins.Tests.Pipeline
{
    public class PluginAssemblyScannerTests : FakeXrmEasyPipelineTestsBase
    {
        public class TestPlugin : IPlugin
        {
            public void Execute(IServiceProvider serviceProvider)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void Should_Register_Plugins_From_Assembly_Using_Config_Provider()
        {
            // Arrange
            var configProvider = A.Fake<IPluginStepConfigProvider>();
            var scanner = new PluginAssemblyScanner(configProvider);
            var assembly = Assembly.GetExecutingAssembly();

            var pluginStepDefinition = new PluginStepDefinition
            {
                MessageName = "Create",
                Stage = ProcessingStepStage.Preoperation,
                Mode = ProcessingStepMode.Synchronous,
                EntityLogicalName = "account"
            };

            A.CallTo(() => configProvider.GetPluginStepDefinitions(typeof(TestPlugin)))
                .Returns(new List<IPluginStepDefinition> { pluginStepDefinition });

            // Act
            scanner.RegisterPluginsFromAssembly(_context, assembly);

            // Assert
            // Verify that the step is registered in the context
            // We can check if SdkMessageProcessingStep exists
            var steps = _context.CreateQuery("sdkmessageprocessingstep").ToList();
            Assert.True(steps.Any(s => s.GetAttributeValue<OptionSetValue>("stage").Value == (int)ProcessingStepStage.Preoperation));
        }
        
        [Fact]
        public void Should_Cache_Registrations()
        {
             // Arrange
            var configProvider = A.Fake<IPluginStepConfigProvider>();
            var scanner = new PluginAssemblyScanner(configProvider);
            var assembly = Assembly.GetExecutingAssembly();

            var pluginStepDefinition = new PluginStepDefinition
            {
                MessageName = "Create",
                Stage = ProcessingStepStage.Preoperation,
                Mode = ProcessingStepMode.Synchronous,
                EntityLogicalName = "account"
            };

            A.CallTo(() => configProvider.GetPluginStepDefinitions(typeof(TestPlugin)))
                .Returns(new List<IPluginStepDefinition> { pluginStepDefinition });

            // Act
            scanner.RegisterPluginsFromAssembly(_context, assembly);
            scanner.RegisterPluginsFromAssembly(_context, assembly);

            // Assert
            // GetPluginStepDefinitions should be called only once per type
            A.CallTo(() => configProvider.GetPluginStepDefinitions(typeof(TestPlugin))).MustHaveHappenedOnceExactly();
        }
    }
}
