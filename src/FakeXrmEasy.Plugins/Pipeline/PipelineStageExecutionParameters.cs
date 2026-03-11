using System.Collections.Generic;
using FakeXrmEasy.Abstractions.Plugins.Enums;
using FakeXrmEasy.Pipeline.Scope;
using FakeXrmEasy.Plugins;
using FakeXrmEasy.Plugins.Extensions;
using Microsoft.Xrm.Sdk;

namespace FakeXrmEasy.Pipeline
{
    internal class PipelineStageExecutionParameters
    {
        internal ProcessingStepStage Stage { get; set; }
        internal ProcessingStepMode Mode { get; set; }

        /// <summary>
        /// Original request that triggered this pipeline execution stage
        /// </summary>
        internal OrganizationRequest Request { get; set; }

        /// <summary>
        /// Original response of the request that triggered this pipeline execution stage
        /// </summary>
        internal OrganizationResponse Response { get; set; }

        /// <summary>
        /// Snapshot of the entity values before the execution of a non-bulk operation request
        /// </summary>
        internal Entity PreEntitySnapshot { get; set; }

        /// <summary>
        /// Snapshot of the entity values after the execution of a non-bulk operation request
        /// </summary>
        internal Entity PostEntitySnapshot { get; set; }
        
        /// <summary>
        /// Snapshot of all the entity preimages before the execution of a bulk operation request
        /// </summary>
        internal List<Entity> PreEntitySnapshotCollection { get; set; }
        
        /// <summary>
        /// Snapshot of all the entity postimages after the execution of a bulk operation request
        /// </summary>
        internal List<Entity> PostEntitySnapshotCollection { get; set; }

        /// <summary>
        /// The current event pipeline scope
        /// </summary>
        internal EventPipelineScope Scope { get; set; }

        /// <summary>
        /// Shared variables for the pipeline execution
        /// </summary>
        internal ParameterCollection SharedVariables { get; set; } = new ParameterCollection();
        
        /// <summary>
        /// Converts the current bulk operation pipeline request parameters into an array of multiple non-bulk operation pipeline execution parameters 
        /// </summary>
        /// <returns></returns>
        internal PipelineStageExecutionParameters[] ToNonBulkPipelineExecutionParameters()
        {
            var requests = Request.ToNonBulkOrganizationRequests();
            var pipelineParameters = new List<PipelineStageExecutionParameters>();
            foreach (var request in requests)
            {
                pipelineParameters.Add(new PipelineStageExecutionParameters()
                {
                    Stage = Stage,
                    Mode = Mode,
                    Request = request
                });
            }

            return pipelineParameters.ToArray();
        }
        
        /// <summary>
        /// Converts the current non-bulk operation pipeline request parameter into another pipeline execution parameter with a bulk operation and a single record
        /// </summary>
        /// <returns></returns>
        internal PipelineStageExecutionParameters ToBulkPipelineExecutionParameters()
        {
            var request = Request.ToBulkOrganizationRequest();
            return new PipelineStageExecutionParameters()
            {
                Stage = Stage,
                Mode = Mode,
                Request = request
            };
        }

        /// <summary>
        /// Creates new pipeline execution parameters from the current organization request
        /// </summary>
        /// <returns></returns>
        internal static PipelineStageExecutionParameters FromOrganizationRequest(OrganizationRequest request)
        {
            EventPipelineScope scope = null;
            
            var pipelineOrganizationRequest = request as PipelineOrganizationRequest;
            if (pipelineOrganizationRequest != null)
            {
                scope = pipelineOrganizationRequest.CurrentScope;
            }

            var originalRequest = pipelineOrganizationRequest != null ? pipelineOrganizationRequest.OriginalRequest : request;

            // This ensures that any early bound entity passed in the request is converted to a late bound entity
            // This is necessary because the pipeline simulation might be running in a context (e.g. plugins) 
            // where the early bound type is not available or different (e.g. shared projects)
            if (originalRequest.Parameters.ContainsKey("Target") && originalRequest.Parameters["Target"] is Entity targetEntity)
            {
                if (targetEntity.GetType() != typeof(Entity))
                {
                    originalRequest.Parameters["Target"] = targetEntity.ToEntity<Entity>();
                }
            }
            
            return new PipelineStageExecutionParameters()
            {
                Request = originalRequest,
                Scope = scope
            };
        }
    }
}
