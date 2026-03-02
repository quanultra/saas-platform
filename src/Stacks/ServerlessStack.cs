using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.IAM;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// Serverless Stack with Lambda functions
    /// Implements Requirement 3.14
    /// </summary>
    public class ServerlessStack : BaseStack
    {
        public Function ProcessorFunction { get; }
        public Function ApiHandlerFunction { get; }
        public Alias ProductionAlias { get; }

        public ServerlessStack(Construct scope, string id, IStackProps props, StackConfiguration config)
            : base(scope, id, props, config)
        {
            // Create separate IAM roles to avoid dependency cycles
            var processorRole = new Role(this, "ProcessorExecutionRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                RoleName = GenerateResourceName("processor-role"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
                    ManagedPolicy.FromAwsManagedPolicyName("AWSXRayDaemonWriteAccess")
                }
            });

            var apiHandlerRole = new Role(this, "ApiHandlerExecutionRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                RoleName = GenerateResourceName("api-handler-role"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
                    ManagedPolicy.FromAwsManagedPolicyName("AWSXRayDaemonWriteAccess")
                }
            });

            // Create Lambda Layer for shared dependencies
            var sharedLayer = new LayerVersion(this, "SharedLayer", new LayerVersionProps
            {
                Code = Code.FromAsset("lambda-layers/shared"),
                CompatibleRuntimes = new[] { Runtime.NODEJS_20_X },
                Description = "Shared utilities layer",
                LayerVersionName = GenerateResourceName("shared-layer")
            });

            // Create Processor Lambda Function with provisioned concurrency
            ProcessorFunction = new Function(this, "ProcessorFunction", new FunctionProps
            {
                Runtime = Runtime.NODEJS_20_X,
                Handler = "index.handler",
                Code = Code.FromInline("exports.handler = async (event) => { console.log('Processing event:', JSON.stringify(event)); if (event.shouldFail) { throw new Error('Task processing failed'); } return { ...event, processed: true, timestamp: new Date().toISOString() }; };"),
                FunctionName = GenerateResourceName("processor"),
                Description = "Event processor function with provisioned concurrency",
                Timeout = Duration.Seconds(30),
                MemorySize = 512,
                Environment = new Dictionary<string, string>
                {
                    ["ENVIRONMENT"] = config.Environment,
                    ["PROJECT_NAME"] = config.ProjectName,
                    ["LOG_LEVEL"] = "INFO"
                },
                Role = processorRole,
                Tracing = Tracing.ACTIVE,
                ReservedConcurrentExecutions = 10,
                Layers = new[] { sharedLayer }
            });

            // Create version and alias with provisioned concurrency
            var version = ProcessorFunction.CurrentVersion;
            ProductionAlias = new Alias(this, "ProcessorProductionAlias", new AliasProps
            {
                AliasName = "production",
                Version = version,
                ProvisionedConcurrentExecutions = 5,
                Description = "Production alias with provisioned concurrency"
            });

            // Create API Handler Lambda Function
            ApiHandlerFunction = new Function(this, "ApiHandlerFunction", new FunctionProps
            {
                Runtime = Runtime.NODEJS_20_X,
                Handler = "index.handler",
                Code = Code.FromInline("exports.handler = async (event) => { console.log('API request:', JSON.stringify(event)); const path = event.path || event.rawPath; const method = event.httpMethod || event.requestContext?.http?.method; if (method === 'GET' && path === '/health') { return { statusCode: 200, body: JSON.stringify({ status: 'healthy' }) }; } if (method === 'POST' && path === '/process') { const body = JSON.parse(event.body || '{}'); return { statusCode: 200, body: JSON.stringify({ message: 'Processing started', data: body }) }; } return { statusCode: 404, body: JSON.stringify({ error: 'Not found' }) }; };"),
                FunctionName = GenerateResourceName("api-handler"),
                Description = "API handler function",
                Timeout = Duration.Seconds(15),
                MemorySize = 256,
                Environment = new Dictionary<string, string>
                {
                    ["ENVIRONMENT"] = config.Environment,
                    ["PROJECT_NAME"] = config.ProjectName,
                    ["PROCESSOR_FUNCTION_ARN"] = ProcessorFunction.FunctionArn
                },
                Role = apiHandlerRole,
                Tracing = Tracing.ACTIVE,
                Layers = new[] { sharedLayer }
            });

            // Grant permissions - now safe because different roles
            ProcessorFunction.GrantInvoke(apiHandlerRole);

            // Create CloudFormation outputs
            CreateOutput("ProcessorFunctionArn", ProcessorFunction.FunctionArn, "Processor Lambda function ARN");
            CreateOutput("ProcessorFunctionName", ProcessorFunction.FunctionName, "Processor Lambda function name");
            CreateOutput("ApiHandlerFunctionArn", ApiHandlerFunction.FunctionArn, "API Handler Lambda function ARN");
            CreateOutput("ApiHandlerFunctionName", ApiHandlerFunction.FunctionName, "API Handler Lambda function name");
            CreateOutput("ProductionAliasArn", ProductionAlias.FunctionArn, "Production alias ARN with provisioned concurrency");
        }
    }
}
