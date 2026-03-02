using Amazon.CDK;
using Amazon.CDK.Assertions;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Stacks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AwsSapC02Practice.Tests.PropertyTests
{
    public class ServerlessPropertyTests
    {
        [FsCheck.Xunit.Property(MaxTest = 20)]
        public FsCheck.Property ApiGatewayRateLimitingShouldBeConfigured()
        {
            var environmentGenerator = Gen.Elements("dev", "staging", "prod");
            return Prop.ForAll(Arb.From(environmentGenerator), env =>
            {
                var app = new App();
                var config = new StackConfiguration { Environment = env, ProjectName = "test-project" };
                var stack = new ApiGatewayStack(app, "TestApiGatewayStack", new StackProps(), config);
                var template = Template.FromStack(stack);
                template.ResourceCountIs("AWS::ApiGateway::RestApi", 1);
                var stages = template.FindResources("AWS::ApiGateway::Stage");
                stages.Should().NotBeEmpty();
                var functions = template.FindResources("AWS::Lambda::Function");
                var hasAuthorizerFunction = functions.Any(f =>
                {
                    var props = f.Value as Dictionary<string, object>;
                    if (props != null && props.ContainsKey("Properties"))
                    {
                        var properties = props["Properties"] as Dictionary<string, object>;
                        if (properties != null && properties.ContainsKey("Description"))
                        {
                            var description = properties["Description"]?.ToString() ?? "";
                            return description.Contains("authorizer");
                        }
                    }
                    return false;
                });
                return hasAuthorizerFunction && stages.Count > 0;
            });
        }

        [FsCheck.Xunit.Property(MaxTest = 20)]
        public FsCheck.Property LambdaFunctionsShouldHaveOptimalConfiguration()
        {
            var environmentGenerator = Gen.Elements("dev", "staging", "prod");
            return Prop.ForAll(Arb.From(environmentGenerator), env =>
            {
                var app = new App();
                var config = new StackConfiguration { Environment = env, ProjectName = "test-project" };
                var stack = new ServerlessStack(app, "TestServerlessStack", new StackProps(), config);
                var template = Template.FromStack(stack);
                var functions = template.FindResources("AWS::Lambda::Function");
                functions.Should().NotBeEmpty();
                functions.Count.Should().BeGreaterThanOrEqualTo(2);
                template.ResourceCountIs("AWS::Lambda::LayerVersion", 1);
                var allFunctionsConfigured = functions.All(f =>
                {
                    var props = f.Value as Dictionary<string, object>;
                    if (props != null && props.ContainsKey("Properties"))
                    {
                        var properties = props["Properties"] as Dictionary<string, object>;
                        if (properties != null)
                        {
                            var hasMemorySize = properties.ContainsKey("MemorySize");
                            var hasTimeout = properties.ContainsKey("Timeout");
                            var hasEnvironment = properties.ContainsKey("Environment");
                            return hasMemorySize && hasTimeout && hasEnvironment;
                        }
                    }
                    return false;
                });
                var aliases = template.FindResources("AWS::Lambda::Alias");
                var hasProvisionedConcurrency = aliases.Any(a =>
                {
                    var props = a.Value as Dictionary<string, object>;
                    if (props != null && props.ContainsKey("Properties"))
                    {
                        var properties = props["Properties"] as Dictionary<string, object>;
                        return properties != null && properties.ContainsKey("ProvisionedConcurrencyConfig");
                    }
                    return false;
                });
                return allFunctionsConfigured && hasProvisionedConcurrency;
            });
        }

        [FsCheck.Xunit.Property(MaxTest = 20)]
        public FsCheck.Property DynamoDbTablesShouldSupportConsistencyAndScaling()
        {
            var environmentGenerator = Gen.Elements("dev", "staging", "prod");
            return Prop.ForAll(Arb.From(environmentGenerator), env =>
            {
                try
                {
                    var app = new App();
                    var config = new StackConfiguration { Environment = env, ProjectName = "test-project" };
                    var stack = new DynamoDbStack(app, "TestDynamoDbStack", new StackProps(), config);
                    var template = Template.FromStack(stack);
                    var tables = template.FindResources("AWS::DynamoDB::Table");
                    if (tables.Count < 2) throw new Exception($"Expected at least 2 tables, found {tables.Count}");
                    var allTablesConfigured = tables.All(t =>
                    {
                        var props = t.Value as Dictionary<string, object>;
                        if (props != null && props.ContainsKey("Properties"))
                        {
                            var properties = props["Properties"] as Dictionary<string, object>;
                            if (properties != null)
                            {
                                var hasEncryption = properties.ContainsKey("SSESpecification");
                                var hasStream = properties.ContainsKey("StreamSpecification");
                                return hasEncryption && hasStream;
                            }
                        }
                        return false;
                    });
                    if (!allTablesConfigured) throw new Exception("Not all tables have encryption and streams");
                    var hasGSI = tables.Any(t =>
                    {
                        var props = t.Value as Dictionary<string, object>;
                        if (props != null && props.ContainsKey("Properties"))
                        {
                            var properties = props["Properties"] as Dictionary<string, object>;
                            if (properties != null && properties.ContainsKey("GlobalSecondaryIndexes"))
                            {
                                var gsis = properties["GlobalSecondaryIndexes"] as object[];
                                return gsis != null && gsis.Length > 0;
                            }
                        }
                        return false;
                    });
                    if (!hasGSI) throw new Exception("No tables have Global Secondary Indexes");
                    var scalingTargets = template.FindResources("AWS::ApplicationAutoScaling::ScalableTarget");
                    if (scalingTargets.Count == 0) throw new Exception("No auto-scaling targets found");
                    var backupPlans = template.FindResources("AWS::Backup::BackupPlan");
                    if (backupPlans.Count != 1) throw new Exception($"Expected 1 backup plan, found {backupPlans.Count}");
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Test failed for environment '{env}': {ex.Message}", ex);
                }
            });
        }
    }
}
