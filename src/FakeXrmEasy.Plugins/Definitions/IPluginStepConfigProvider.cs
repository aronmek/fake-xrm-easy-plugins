using System;
using System.Collections.Generic;

namespace FakeXrmEasy.Plugins.Definitions
{
    /// <summary>
    /// Interface for providing plugin step definitions for a given plugin type.
    /// </summary>
    public interface IPluginStepConfigProvider
    {
        /// <summary>
        /// Gets the plugin step definitions for the specified plugin type.
        /// </summary>
        /// <param name="pluginType">The type of the plugin.</param>
        /// <returns>A collection of plugin step definitions.</returns>
        IEnumerable<IPluginStepDefinition> GetPluginStepDefinitions(Type pluginType);
    }
}
