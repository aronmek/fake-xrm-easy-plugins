using System;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Pipeline;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace FakeXrmEasy.Plugins.Extensions
{
    public static class XrmFakedContextAsyncExtensions
    {
        public static IAsyncPluginExecution CreateWithTracking(this IXrmFakedContext context, Entity entity)
        {
            var request = new CreateRequest { Target = entity };
            return context.ExecuteWithTracking(request);
        }

        public static IAsyncPluginExecution UpdateWithTracking(this IXrmFakedContext context, Entity entity)
        {
            var request = new UpdateRequest { Target = entity };
            return context.ExecuteWithTracking(request);
        }

        public static IAsyncPluginExecution DeleteWithTracking(this IXrmFakedContext context, string logicalName, Guid id)
        {
            var request = new DeleteRequest { Target = new EntityReference(logicalName, id) };
            return context.ExecuteWithTracking(request);
        }

        public static IAsyncPluginExecution ExecuteWithTracking(this IXrmFakedContext context, OrganizationRequest request)
        {
            if (!context.HasProperty<IAsyncPluginBackgroundTaskManager>())
            {
                context.SetProperty<IAsyncPluginBackgroundTaskManager>(new AsyncPluginBackgroundTaskManager());
            }
            var manager = context.GetProperty<IAsyncPluginBackgroundTaskManager>();
            
            using(var scope = manager.BeginScope())
            {
                var service = context.GetOrganizationService();
                service.Execute(request);
                
                return new AsyncPluginExecution(manager, scope.Id);
            }
        }
    }
}
