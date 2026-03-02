using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.IAM;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// API Gateway Stack with REST API
    /// Implements Requirement 3.13
    /// </summary>
    public class ApiGatewayStack : BaseStack
    {
        public RestApi RestApi { get; }
        public IAuthorizer TokenAuthorizer { get; }
        public RequestValidator RequestValidator { get; }

        public ApiGatewayStack(Construct scope, string id, IStackProps props, StackConfiguration config)
            : base(scope, id, props, config)
        {
            // Create a simple Lambda function for authorizer
            var authorizerFunction = new Function(this, "AuthorizerFunction", new FunctionProps
            {
                Runtime = Runtime.NODEJS_20_X,
                Handler = "index.handler",
                Code = Code.FromInline("exports.handler = async (event) => { const token = event.authorizationToken; return { principalId: 'user', policyDocument: { Version: '2012-10-17', Statement: [{ Action: 'execute-api:Invoke', Effect: token === 'allow' ? 'Allow' : 'Deny', Resource: event.methodArn }] } }; };"),
                Description = "Token authorizer for API Gateway"
            });

            // Create REST API with throttling and caching
            RestApi = new RestApi(this, "RestApi", new RestApiProps
            {
                RestApiName = GenerateResourceName("rest-api"),
                Description = "REST API with authorizers, validators, throttling, and caching",
                DeployOptions = new StageOptions
                {
                    StageName = config.Environment,
                    ThrottlingRateLimit = 1000,
                    ThrottlingBurstLimit = 2000,
                    CachingEnabled = true,
                    CacheTtl = Duration.Minutes(5),
                    CacheClusterEnabled = true,
                    CacheClusterSize = "0.5",
                    MetricsEnabled = true,
                    LoggingLevel = MethodLoggingLevel.INFO,
                    DataTraceEnabled = true
                },
                CloudWatchRole = true,
                DefaultCorsPreflightOptions = new CorsOptions
                {
                    AllowOrigins = Cors.ALL_ORIGINS,
                    AllowMethods = Cors.ALL_METHODS
                }
            });

            // Create Token Authorizer
            TokenAuthorizer = new TokenAuthorizer(this, "TokenAuthorizer", new TokenAuthorizerProps
            {
                Handler = authorizerFunction,
                IdentitySource = "method.request.header.Authorization",
                ResultsCacheTtl = Duration.Minutes(5)
            });

            // Create Request Validator
            RequestValidator = new RequestValidator(this, "RequestValidator", new RequestValidatorProps
            {
                RestApi = RestApi,
                RequestValidatorName = GenerateResourceName("request-validator"),
                ValidateRequestBody = true,
                ValidateRequestParameters = true
            });

            // Create a sample resource with method
            var itemsResource = RestApi.Root.AddResource("items");
            itemsResource.AddMethod("GET", new MockIntegration(new IntegrationOptions
            {
                IntegrationResponses = new[]
                {
                    new IntegrationResponse
                    {
                        StatusCode = "200",
                        ResponseTemplates = new Dictionary<string, string>
                        {
                            ["application/json"] = "{\"message\": \"Success\"}"
                        }
                    }
                },
                PassthroughBehavior = PassthroughBehavior.NEVER,
                RequestTemplates = new Dictionary<string, string>
                {
                    ["application/json"] = "{\"statusCode\": 200}"
                }
            }), new MethodOptions
            {
                Authorizer = TokenAuthorizer,
                RequestValidator = RequestValidator,
                MethodResponses = new[]
                {
                    new MethodResponse { StatusCode = "200" }
                }
            });

            // Create CloudFormation outputs
            CreateOutput("RestApiId", RestApi.RestApiId, "REST API ID");
            CreateOutput("RestApiUrl", RestApi.Url, "REST API URL");
            CreateOutput("AuthorizerFunctionArn", authorizerFunction.FunctionArn, "Authorizer Lambda function ARN");
        }
    }
}
