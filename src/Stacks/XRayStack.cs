using Amazon.CDK;
using XRay = Amazon.CDK.AWS.XRay;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.ECS;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// X-Ray Stack with sampling rules, service maps, and trace analysis
    /// Implements Requirement 12.5
    /// </summary>
    public class XRayStack : BaseStack
    {
        public XRay.CfnSamplingRule DefaultSamplingRule { get; }
        public XRay.CfnSamplingRule HighPrioritySamplingRule { get; }
        public XRay.CfnSamplingRule DebugSamplingRule { get; }
        public XRay.CfnGroup ErrorTracesGroup { get; }
        public XRay.CfnGroup SlowTracesGroup { get; }
        public XRay.CfnGroup CriticalPathGroup { get; }
        public Role XRayRole { get; }

        public XRayStack(Construct scope, string id, IStackProps props, StackConfiguration config)
            : base(scope, id, props, config)
        {
            // Create IAM role for X-Ray
            XRayRole = new Role(this, "XRayRole", new RoleProps
            {
                RoleName = GenerateResourceName("xray-role"),
                AssumedBy = new ServicePrincipal("xray.amazonaws.com"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("AWSXRayDaemonWriteAccess")
                }
            });

            // Create default sampling rule (low sampling rate for general traffic)
            DefaultSamplingRule = new XRay.CfnSamplingRule(this, "DefaultSamplingRule", new XRay.CfnSamplingRuleProps
            {
                RuleName = GenerateResourceName("default-sampling"),
                SamplingRule = new XRay.CfnSamplingRule.SamplingRuleProperty
                {
                    RuleName = GenerateResourceName("default-sampling"),
                    Priority = 1000,
                    FixedRate = 0.05, // 5% sampling rate
                    ReservoirSize = 1,
                    ServiceName = "*",
                    ServiceType = "*",
                    Host = "*",
                    HttpMethod = "*",
                    UrlPath = "*",
                    ResourceArn = "*",
                    Version = 1
                }
            });

            // Create high priority sampling rule (higher sampling for critical endpoints)
            HighPrioritySamplingRule = new XRay.CfnSamplingRule(this, "HighPrioritySamplingRule", new XRay.CfnSamplingRuleProps
            {
                RuleName = GenerateResourceName("high-priority-sampling"),
                SamplingRule = new XRay.CfnSamplingRule.SamplingRuleProperty
                {
                    RuleName = GenerateResourceName("high-priority-sampling"),
                    Priority = 100,
                    FixedRate = 0.5, // 50% sampling rate
                    ReservoirSize = 50,
                    ServiceName = "*",
                    ServiceType = "*",
                    Host = "*",
                    HttpMethod = "*",
                    UrlPath = "/api/critical/*",
                    ResourceArn = "*",
                    Version = 1
                }
            });

            // Create debug sampling rule (100% sampling for debugging)
            DebugSamplingRule = new XRay.CfnSamplingRule(this, "DebugSamplingRule", new XRay.CfnSamplingRuleProps
            {
                RuleName = GenerateResourceName("debug-sampling"),
                SamplingRule = new XRay.CfnSamplingRule.SamplingRuleProperty
                {
                    RuleName = GenerateResourceName("debug-sampling"),
                    Priority = 1,
                    FixedRate = 1.0, // 100% sampling rate
                    ReservoirSize = 100,
                    ServiceName = "*",
                    ServiceType = "*",
                    Host = "*",
                    HttpMethod = "*",
                    UrlPath = "/api/debug/*",
                    ResourceArn = "*",
                    Version = 1
                }
            });

            // Create error sampling rule (100% sampling for errors)
            new XRay.CfnSamplingRule(this, "ErrorSamplingRule", new XRay.CfnSamplingRuleProps
            {
                RuleName = GenerateResourceName("error-sampling"),
                SamplingRule = new XRay.CfnSamplingRule.SamplingRuleProperty
                {
                    RuleName = GenerateResourceName("error-sampling"),
                    Priority = 10,
                    FixedRate = 1.0, // 100% sampling for errors
                    ReservoirSize = 50,
                    ServiceName = "*",
                    ServiceType = "*",
                    Host = "*",
                    HttpMethod = "*",
                    UrlPath = "*",
                    ResourceArn = "*",
                    Version = 1,
                    Attributes = new Dictionary<string, string>
                    {
                        ["error"] = "true"
                    }
                }
            });

            // Create slow request sampling rule (100% sampling for slow requests)
            new XRay.CfnSamplingRule(this, "SlowRequestSamplingRule", new XRay.CfnSamplingRuleProps
            {
                RuleName = GenerateResourceName("slow-request-sampling"),
                SamplingRule = new XRay.CfnSamplingRule.SamplingRuleProperty
                {
                    RuleName = GenerateResourceName("slow-request-sampling"),
                    Priority = 20,
                    FixedRate = 1.0, // 100% sampling for slow requests
                    ReservoirSize = 25,
                    ServiceName = "*",
                    ServiceType = "*",
                    Host = "*",
                    HttpMethod = "*",
                    UrlPath = "*",
                    ResourceArn = "*",
                    Version = 1,
                    Attributes = new Dictionary<string, string>
                    {
                        ["slow"] = "true"
                    }
                }
            });

            // Create X-Ray group for error traces
            ErrorTracesGroup = new XRay.CfnGroup(this, "ErrorTracesGroup", new XRay.CfnGroupProps
            {
                GroupName = GenerateResourceName("error-traces"),
                FilterExpression = "error = true OR fault = true"
            });

            // Create X-Ray group for slow traces (>3 seconds)
            SlowTracesGroup = new XRay.CfnGroup(this, "SlowTracesGroup", new XRay.CfnGroupProps
            {
                GroupName = GenerateResourceName("slow-traces"),
                FilterExpression = "duration >= 3"
            });

            // Create X-Ray group for critical path traces
            CriticalPathGroup = new XRay.CfnGroup(this, "CriticalPathGroup", new XRay.CfnGroupProps
            {
                GroupName = GenerateResourceName("critical-path"),
                FilterExpression = "service(\"api\") { http.url CONTAINS \"/api/critical\" }"
            });

            // Create X-Ray group for database queries
            new XRay.CfnGroup(this, "DatabaseQueriesGroup", new XRay.CfnGroupProps
            {
                GroupName = GenerateResourceName("database-queries"),
                FilterExpression = "annotation.database_type EXISTS"
            });

            // Create X-Ray group for external API calls
            new XRay.CfnGroup(this, "ExternalApiCallsGroup", new XRay.CfnGroupProps
            {
                GroupName = GenerateResourceName("external-api-calls"),
                FilterExpression = "http.url BEGINSWITH \"https://api.external\""
            });

            // Create X-Ray group for high latency database operations
            new XRay.CfnGroup(this, "HighLatencyDbGroup", new XRay.CfnGroupProps
            {
                GroupName = GenerateResourceName("high-latency-db"),
                FilterExpression = "annotation.database_type EXISTS AND duration >= 1"
            });

            // Create X-Ray group for authentication traces
            new XRay.CfnGroup(this, "AuthenticationTracesGroup", new XRay.CfnGroupProps
            {
                GroupName = GenerateResourceName("authentication-traces"),
                FilterExpression = "http.url CONTAINS \"/auth/\" OR annotation.auth_event EXISTS"
            });

            // Create outputs
            CreateOutput("XRayRoleArn", XRayRole.RoleArn, "X-Ray IAM role ARN");
            CreateOutput("DefaultSamplingRuleArn", DefaultSamplingRule.AttrRuleArn, "Default sampling rule ARN");
            CreateOutput("HighPrioritySamplingRuleArn", HighPrioritySamplingRule.AttrRuleArn, "High priority sampling rule ARN");
            CreateOutput("DebugSamplingRuleArn", DebugSamplingRule.AttrRuleArn, "Debug sampling rule ARN");
            CreateOutput("ErrorTracesGroupArn", ErrorTracesGroup.AttrGroupArn, "Error traces group ARN");
            CreateOutput("SlowTracesGroupArn", SlowTracesGroup.AttrGroupArn, "Slow traces group ARN");
            CreateOutput("CriticalPathGroupArn", CriticalPathGroup.AttrGroupArn, "Critical path group ARN");
        }

        /// <summary>
        /// Helper method to enable X-Ray tracing on a Lambda function
        /// </summary>
        public void EnableXRayForLambda(Function function)
        {
            function.AddEnvironment("AWS_XRAY_TRACING_NAME", function.FunctionName);
            function.AddEnvironment("AWS_XRAY_CONTEXT_MISSING", "LOG_ERROR");

            function.Role?.AddManagedPolicy(
                ManagedPolicy.FromAwsManagedPolicyName("AWSXRayDaemonWriteAccess")
            );
        }

        /// <summary>
        /// Helper method to get X-Ray daemon configuration for ECS tasks
        /// </summary>
        public Dictionary<string, object> GetXRayDaemonContainerDefinition()
        {
            return new Dictionary<string, object>
            {
                ["name"] = "xray-daemon",
                ["image"] = "amazon/aws-xray-daemon:latest",
                ["cpu"] = 32,
                ["memoryReservation"] = 256,
                ["portMappings"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["containerPort"] = 2000,
                        ["protocol"] = "udp"
                    }
                },
                ["logConfiguration"] = new Dictionary<string, object>
                {
                    ["logDriver"] = "awslogs",
                    ["options"] = new Dictionary<string, string>
                    {
                        ["awslogs-group"] = "/ecs/xray-daemon",
                        ["awslogs-region"] = this.Region,
                        ["awslogs-stream-prefix"] = "xray"
                    }
                }
            };
        }
    }
}
