using System;
using DataverseEntities;
using FakeXrmEasy.Tests.PluginsForTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Xunit;
using FakeXrmEasy.Abstractions.Plugins.Enums;
using FakeXrmEasy.Pipeline;
using FakeXrmEasy.Plugins.PluginSteps;

namespace FakeXrmEasy.Plugins.Tests.Pipeline
{
    public class PipelineTagParameterTests : FakeXrmEasyPipelineTestsBase
    {
        [Fact]
        public void Should_copy_tag_parameter_to_shared_variables()
        {
            var account = new Account { Id = Guid.NewGuid() };
            _context.Initialize(account);
            
            _context.RegisterPluginStep<TagParameterPlugin, Account>(
                "Update", 
                ProcessingStepStage.Preoperation, 
                ProcessingStepMode.Synchronous);

            var updateRequest = new UpdateRequest
            {
                Target = new Account { Id = account.Id }
            };
            
            updateRequest.Parameters.Add("tag", "TestTagValue");

            _service.Execute(updateRequest);
        }
    }
}
