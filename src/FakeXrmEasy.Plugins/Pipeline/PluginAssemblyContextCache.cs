using System.Collections.Concurrent;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.Plugins.Pipeline
{
    /// <summary>
    /// Caches OrganizationService wrappers for specific plugin assemblies to avoid recreation.
    /// </summary>
    public class PluginAssemblyContextCache : ConcurrentDictionary<string, IOrganizationService>
    {
    }
}
