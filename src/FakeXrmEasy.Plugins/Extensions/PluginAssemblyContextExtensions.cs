using System;
using System.Reflection;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Plugins.Pipeline;
using FakeXrmEasy.Plugins.Services;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.Plugins.Extensions
{
    public static class PluginAssemblyContextExtensions
    {
        public static IOrganizationService GetOrganizationService(this IXrmFakedContext context, Assembly pluginAssembly)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (pluginAssembly == null) throw new ArgumentNullException(nameof(pluginAssembly));

            if (!context.HasProperty<PluginAssemblyContextCache>())
            {
                context.SetProperty(new PluginAssemblyContextCache());
            }

            var cache = context.GetProperty<PluginAssemblyContextCache>();
            var assemblyName = pluginAssembly.GetName().Name;

            return cache.GetOrAdd(assemblyName, (key) => 
            {
                return new ProxyTypesOrganizationService(context.GetOrganizationService(), pluginAssembly);
            });
        }
    }
}
