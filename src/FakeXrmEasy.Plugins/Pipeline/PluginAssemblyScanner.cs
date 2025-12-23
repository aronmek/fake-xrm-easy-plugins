using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Plugins.Definitions;
using FakeXrmEasy.Pipeline;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.Plugins.Pipeline
{
    /// <summary>
    /// Scans assemblies for plugins and registers them using a config provider.
    /// </summary>
    public class PluginAssemblyScanner
    {
        private readonly IPluginStepConfigProvider _configProvider;
        private readonly Dictionary<Assembly, List<Tuple<Type, IPluginStepDefinition>>> _cache = new Dictionary<Assembly, List<Tuple<Type, IPluginStepDefinition>>>();

        /// <summary>
        /// Creates a new instance of PluginAssemblyScanner
        /// </summary>
        /// <param name="configProvider">Provider for retrieving plugin step configurations</param>
        public PluginAssemblyScanner(IPluginStepConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        /// <summary>
        /// Registers all plugins from the specified assembly
        /// </summary>
        /// <param name="context">The context to register plugins in</param>
        /// <param name="assembly">The assembly to scan for plugins</param>
        public void RegisterPluginsFromAssembly(IXrmFakedContext context, Assembly assembly)
        {
            if (!_cache.ContainsKey(assembly))
            {
                var definitions = new List<Tuple<Type, IPluginStepDefinition>>();
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);

                foreach (var pluginType in pluginTypes)
                {
                    var steps = _configProvider.GetPluginStepDefinitions(pluginType);
                    if (steps != null)
                    {
                        foreach (var step in steps)
                        {
                            definitions.Add(new Tuple<Type, IPluginStepDefinition>(pluginType, step));
                        }
                    }
                }
                _cache[assembly] = definitions;
            }

            foreach (var def in _cache[assembly])
            {
                context.RegisterPluginStep(def.Item1, def.Item2);
            }
        }
    }
}
