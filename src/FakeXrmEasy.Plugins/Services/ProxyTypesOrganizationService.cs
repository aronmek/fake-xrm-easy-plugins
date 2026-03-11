using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;

namespace FakeXrmEasy.Plugins.Services
{
    /// <summary>
    /// A proxy service that converts Entities to their corresponding early-bound types defined in a target assembly.
    /// This is useful when the context returns generic Entities (e.g. SuppressProxyTypesForRetrieveMultiple=true)
    /// but the plugin expects specific proxy types.
    /// </summary>
    public class ProxyTypesOrganizationService : IOrganizationService
    {
        private readonly IOrganizationService _service;
        private readonly IEnumerable<Assembly> _proxyTypesAssemblies;
        
        // Static cache to store type mappings per assembly
        // Key: Assembly Full Name + Entity Logical Name
        private static readonly ConcurrentDictionary<string, Type> _typeCache = new ConcurrentDictionary<string, Type>();

        // Cache to store which assemblies have been scanned
        private static readonly ConcurrentDictionary<string, bool> _scannedAssemblies = new ConcurrentDictionary<string, bool>();

        public ProxyTypesOrganizationService(IOrganizationService service, Assembly proxyTypesAssembly)
            : this(service, new[] { proxyTypesAssembly })
        {
        }

        public ProxyTypesOrganizationService(IOrganizationService service, IEnumerable<Assembly> proxyTypesAssemblies)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _proxyTypesAssemblies = proxyTypesAssemblies ?? Enumerable.Empty<Assembly>();
            
            ScanAssemblies(_proxyTypesAssemblies);
        }

        private static void ScanAssemblies(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                if (assembly == null) continue;

                if (_scannedAssemblies.TryAdd(assembly.FullName, true))
                {
                    try
                    {
                        var entityTypes = assembly.GetTypes()
                            .Where(t => typeof(Entity).IsAssignableFrom(t));

                        foreach (var type in entityTypes)
                        {
                            var logicalNameAttribute = type.GetCustomAttribute<EntityLogicalNameAttribute>();
                            if (logicalNameAttribute != null && !string.IsNullOrWhiteSpace(logicalNameAttribute.LogicalName))
                            {
                                var key = $"{assembly.FullName}:{logicalNameAttribute.LogicalName}";
                                _typeCache.TryAdd(key, type);
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // Handle cases where some types cannot be loaded (e.g. missing dependencies)
                        // Currently swallowing, but could be logged if tracing was available.
                    }
                }
            }
        }

        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            _service.Associate(entityName, entityId, relationship, relatedEntities);
        }

        public Guid Create(Entity entity)
        {
            var proxy = ConvertToProxy(entity);
            return _service.Create(proxy);
        }

        public void Delete(string entityName, Guid id)
        {
            _service.Delete(entityName, id);
        }

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            _service.Disassociate(entityName, entityId, relationship, relatedEntities);
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            if (request.Parameters.ContainsKey("Target") && request.Parameters["Target"] is Entity target)
            {
                request.Parameters["Target"] = ConvertToProxy(target);
            }

            var response = _service.Execute(request);
            
            if (response is RetrieveMultipleResponse retrieveMultipleResponse)
            {
                ConvertCollection(retrieveMultipleResponse.EntityCollection);
            }

            return response;
        }

        public Entity Retrieve(string entityName, Guid id, Microsoft.Xrm.Sdk.Query.ColumnSet columnSet)
        {
            var entity = _service.Retrieve(entityName, id, columnSet);
            return ConvertToProxy(entity, entityName);
        }

        public EntityCollection RetrieveMultiple(Microsoft.Xrm.Sdk.Query.QueryBase query)
        {
            var collection = _service.RetrieveMultiple(query);
            ConvertCollection(collection);
            return collection;
        }

        public void Update(Entity entity)
        {
            var proxy = ConvertToProxy(entity);
            _service.Update(proxy);
        }

        private void ConvertCollection(EntityCollection collection)
        {
            if (collection?.Entities == null) return;

            for (int i = 0; i < collection.Entities.Count; i++)
            {
                collection.Entities[i] = ConvertToProxy(collection.Entities[i]);
            }
        }

        private Entity ConvertToProxy(Entity entity, string logicalName = null)
        {
            if (entity == null) return null;

            var proxyType = GetProxyType(logicalName ?? entity.LogicalName);
            if (proxyType == null) return entity;

            // If it's already the correct type (or a subclass/proxy of it), simple return
            if (proxyType.IsAssignableFrom(entity.GetType())) return entity;

            try
            {
                var method = typeof(Entity).GetMethod("ToEntity");
                var generic = method.MakeGenericMethod(proxyType);
                return (Entity)generic.Invoke(entity, null);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is NotSupportedException)
            {
                // Fallback for when ToEntity complains about LogicalName (e.g. with certain proxies)
                var instance = (Entity)Activator.CreateInstance(proxyType);
                instance.Id = entity.Id;
                if (!string.IsNullOrEmpty(entity.LogicalName))
                {
                    instance.LogicalName = entity.LogicalName;
                }
                
                foreach(var attribute in entity.Attributes)
                {
                    instance.Attributes[attribute.Key] = attribute.Value;
                }

                instance.EntityState = entity.EntityState;
                instance.RowVersion = entity.RowVersion;
                return instance;
            }
        }

        private Type GetProxyType(string logicalName)
        {
            foreach (var assembly in _proxyTypesAssemblies)
            {
                var key = $"{assembly.FullName}:{logicalName}";
                if (_typeCache.TryGetValue(key, out var type))
                {
                    return type;
                }
            }

            return null;
        }
    }
}
