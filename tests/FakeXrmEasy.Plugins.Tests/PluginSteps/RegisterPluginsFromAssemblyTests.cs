using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Abstractions.Enums;
using FakeXrmEasy.Abstractions.Plugins.Enums;
using FakeXrmEasy.Middleware;
using FakeXrmEasy.Middleware.Pipeline;
using FakeXrmEasy.Pipeline;
using FakeXrmEasy.Plugins.Definitions;
using FakeXrmEasy.Plugins.PluginSteps;
using FakeXrmEasy.Tests.PluginsForTesting;
using Xunit;
using Crm;

namespace FakeXrmEasy.Plugins.Tests.PluginSteps
{
    public class RegisterPluginsFromAssemblyTests
    {
        private IXrmFakedContext _context;

        public RegisterPluginsFromAssemblyTests()
        {
            _context = MiddlewareBuilder
                        .New()
                        .AddPipelineSimulation(new PipelineOptions() { UsePipelineSimulation = true })
                        .SetLicense(FakeXrmEasyLicense.RPL_1_5)
                        .Build();
        }

        [Fact]
        public void Should_register_plugins_from_assembly_using_config_provider()
        {
            var assembly = typeof(AccountNumberPlugin).Assembly;
            var configProvider = A.Fake<IPluginStepConfigProvider>();
            
            var stepDefinition = new PluginStepDefinition
            {
                MessageName = "Create",
                EntityLogicalName = "account",
                Stage = ProcessingStepStage.Preoperation,
                Mode = ProcessingStepMode.Synchronous
            };

            A.CallTo(() => configProvider.GetPluginStepDefinitions(typeof(AccountNumberPlugin)))
                .Returns(new List<IPluginStepDefinition> { stepDefinition });

            _context.RegisterPluginsFromAssembly(assembly, configProvider);

            // Verify registration
            var steps = _context.CreateQuery<SdkMessageProcessingStep>().ToList();
            
            // There might be other plugins in the assembly, but we only mocked config for AccountNumberPlugin
            // If the scanner scans all types, it will call GetPluginStepDefinitions for all of them.
            // Our mock returns default (null or empty) for others.
            // So we should expect at least one step.
            
            var registeredStep = steps.FirstOrDefault(s => s.Stage.Value == (int)ProcessingStepStage.Preoperation);
            Assert.NotNull(registeredStep);
            
            var sdkMessageFilter = _context.CreateQuery<SdkMessageFilter>()
                                    .FirstOrDefault(f => f.Id == registeredStep.SdkMessageFilterId.Id);
            Assert.NotNull(sdkMessageFilter);
            Assert.Equal("account", sdkMessageFilter["entitylogicalname"]);
        }
    }
}
