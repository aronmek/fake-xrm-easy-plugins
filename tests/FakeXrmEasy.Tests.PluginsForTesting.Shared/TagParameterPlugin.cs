using System;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.Tests.PluginsForTesting
{
    public class TagParameterPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            
            if (context.SharedVariables.Contains("tag"))
            {
                var tagValue = (string)context.SharedVariables["tag"];
                if (tagValue != "TestTagValue")
                {
                    throw new InvalidPluginExecutionException($"Expected tag value 'TestTagValue' but found '{tagValue}'");
                }
            }
            else
            {
                throw new InvalidPluginExecutionException("Expected 'tag' in SharedVariables but it was not found");
            }
        }
    }
}
