using System;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Pipeline.Scope;
using FakeXrmEasy.Plugins.PluginSteps;

namespace FakeXrmEasy.Pipeline
{
    public class PipelineEvents : IPipelineEvents
    {
        public event EventHandler<PluginStepEventArgs> OnPluginStepStart;
        public event EventHandler<PluginStepEventArgs> OnPluginStepEnd;

        public void RaiseOnPluginStepStart(IXrmFakedContext context, PluginStepDefinition step, EventPipelineScope scope)
        {
            OnPluginStepStart?.Invoke(context, new PluginStepEventArgs { PluginStep = step, Scope = scope });
        }

        public void RaiseOnPluginStepEnd(IXrmFakedContext context, PluginStepDefinition step, EventPipelineScope scope)
        {
            OnPluginStepEnd?.Invoke(context, new PluginStepEventArgs { PluginStep = step, Scope = scope });
        }
    }
}
