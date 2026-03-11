using System;
using System.Reflection;
using FakeItEasy;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Plugins.Extensions;
using FakeXrmEasy.Plugins.Pipeline;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace FakeXrmEasy.Plugins.Tests.Extensions
{
    public class PluginAssemblyContextExtensionsTests
    {
        [Fact]
        public void Should_Return_Cached_Service_If_Available()
        {
            // Arrange
            var context = A.Fake<IXrmFakedContext>();
            var cache = new PluginAssemblyContextCache();
            var cachedService = A.Fake<IOrganizationService>();
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName().Name;

            cache.TryAdd(assemblyName, cachedService);
            
            A.CallTo(() => context.GetProperty<PluginAssemblyContextCache>()).Returns(cache);
            A.CallTo(() => context.HasProperty<PluginAssemblyContextCache>()).Returns(true);

            // Act
            var service = context.GetOrganizationService(assembly);

            // Assert
            Assert.Same(cachedService, service);
        }

        [Fact]
        public void Should_Create_And_Cache_New_Service_If_Not_Available()
        {
            // Arrange
            var context = A.Fake<IXrmFakedContext>();
            var baseService = A.Fake<IOrganizationService>();
            var assembly = Assembly.GetExecutingAssembly();
            
            // Context behaves as having no cache initially, or empty cache
            // But checking HasProperty is tricky with Fakes if we rely on the extension to Create the cache.
            // Let's assume the extension handles cache creation.
            
            // We use a real variable to simulate the property bag
            var properties = new System.Collections.Generic.Dictionary<string, object>();
            
            A.CallTo(() => context.SetProperty(A<PluginAssemblyContextCache>._))
                .Invokes((PluginAssemblyContextCache c) => properties[typeof(PluginAssemblyContextCache).FullName] = c);

            A.CallTo(() => context.GetProperty<PluginAssemblyContextCache>())
                .ReturnsLazily(() => properties.ContainsKey(typeof(PluginAssemblyContextCache).FullName) 
                    ? (PluginAssemblyContextCache)properties[typeof(PluginAssemblyContextCache).FullName] 
                    : null);
            
             A.CallTo(() => context.HasProperty<PluginAssemblyContextCache>())
                .ReturnsLazily(() => properties.ContainsKey(typeof(PluginAssemblyContextCache).FullName));

            A.CallTo(() => context.GetOrganizationService()).Returns(baseService);

            // Act
            var service = context.GetOrganizationService(assembly);

            // Assert
            Assert.NotSame(baseService, service); // Should be wrapped
            Assert.True(properties.ContainsKey(typeof(PluginAssemblyContextCache).FullName));
            var cache = (PluginAssemblyContextCache)properties[typeof(PluginAssemblyContextCache).FullName];
            Assert.True(cache.ContainsKey(assembly.GetName().Name));
            Assert.Same(service, cache[assembly.GetName().Name]);
        }
    }
}
