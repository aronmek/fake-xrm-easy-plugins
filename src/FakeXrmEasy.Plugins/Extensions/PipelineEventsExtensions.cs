using System;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Pipeline;

namespace FakeXrmEasy.Extensions
{
    public static class PipelineEventsExtensions
    {
        public static IPipelineEvents EnablePipelineEvents(this IXrmFakedContext context)
        {
            var events = new PipelineEvents();
            if(!context.HasProperty<IPipelineEvents>())
            {
                context.SetProperty<IPipelineEvents>(events);
            }
            return context.GetProperty<IPipelineEvents>(); 
        }

        public static IPipelineEvents GetPipelineEvents(this IXrmFakedContext context)
        {
            return context.GetProperty<IPipelineEvents>();
        }
    }
}
