using System;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Pipeline.Scope;
using FakeXrmEasy.Plugins.PluginSteps;

namespace FakeXrmEasy.Pipeline
{
    public class PluginStepEventArgs : EventArgs
    {
        public PluginStepDefinition PluginStep { get; set; }
        public EventPipelineScope Scope { get; set; }
    }

    public interface IPipelineEvents
    {
        event EventHandler<PluginStepEventArgs> OnPluginStepStart;
        event EventHandler<PluginStepEventArgs> OnPluginStepEnd;
        
        void RaiseOnPluginStepStart(IXrmFakedContext context, PluginStepDefinition step, EventPipelineScope scope);
        void RaiseOnPluginStepEnd(IXrmFakedContext context, PluginStepDefinition step, EventPipelineScope scope);
    }
}
