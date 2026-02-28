using Amazon.CDK;
using Amazon.CDK.Assertions;
using Amazon.CDK.AWS.EC2;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Stacks;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace AwsSapC02Practice.Tests.Unit
{
    /// <summary>
    /// Tests for ECS Fargate Stack
    /// Validates Requirement 11.3 - Test ECS service deployment
    /// </summary>
    public class EcsStackTests
    {
        private readonly StackConfiguration _testConfig;
        private readonly App _app;
        private readonly IVpc _vpc;
        private readonly ISecurityGroup _securityGroup;

        public EcsStackTests()
        {
            _testConfig = new StackConfiguration
            {
                Environment = "test",
                ProjectName = "test-project",

            };
            _app = new App();

            // Create VPC for testing
            var vpcStack = new VpcStack(_app, "TestVpcStack", new StackProps(), _testConfig);
            _vpc = vpcStack.PrimaryVpc.Vpc;
            _securityGroup = vpcStack.PrimaryVpc.ApplicationSecurityGroup;
        }

        [Fact]
        public void EcsStack_ShouldCreateCluster()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);
            var template = Template.FromStack(stack);

            stack.Cluster.Should().NotBeNull();
            // Verify cluster is created with correct name pattern
            template.HasResourceProperties("AWS::ECS::Cluster", new Dictionary<string, object>
            {
                ["ClusterName"] = Match.StringLikeRegexp(".*test-project.*")
            });
        }


        [Fact]
        public void EcsStack_ShouldCreateFargateService()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);
            var template = Template.FromStack(stack);

            stack.FargateService.Should().NotBeNull();
            // Verify service is created with correct name pattern
            template.HasResourceProperties("AWS::ECS::Service", new Dictionary<string, object>
            {
                ["ServiceName"] = Match.StringLikeRegexp(".*fargate-service.*")
            });
        }

        [Fact]
        public void EcsStack_ShouldCreateServiceDiscoveryNamespace()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);

            stack.ServiceNamespace.Should().NotBeNull();
            stack.ServiceNamespace.NamespaceName.Should().Contain("test-project.local");
        }

        [Fact]
        public void EcsStack_ShouldCreateClusterWithContainerInsights()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::ECS::Cluster", new Dictionary<string, object>
            {
                ["ClusterSettings"] = Match.ArrayWith(new[]
                {
                    new Dictionary<string, string>
                    {
                        ["Name"] = "containerInsights",
                        ["Value"] = "enabled"
                    }
                })
            });
        }

        [Fact]
        public void EcsStack_ShouldCreateFargateTaskDefinition()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::ECS::TaskDefinition", new Dictionary<string, object>
            {
                ["RequiresCompatibilities"] = new[] { "FARGATE" },
                ["NetworkMode"] = "awsvpc",
                ["Cpu"] = "256",
                ["Memory"] = "512"
            });
        }

        [Fact]
        public void EcsStack_ShouldCreateServiceWithDesiredCount()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::ECS::Service", new Dictionary<string, object>
            {
                ["DesiredCount"] = 2,
                ["LaunchType"] = "FARGATE"
            });
        }

        [Fact]
        public void EcsStack_ShouldCreateLogGroup()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::Logs::LogGroup", new Dictionary<string, object>
            {
                ["RetentionInDays"] = 7
            });
        }

        [Fact]
        public void EcsStack_ShouldCreateTargetGroup()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::ElasticLoadBalancingV2::TargetGroup", new Dictionary<string, object>
            {
                ["Port"] = 80,
                ["Protocol"] = "HTTP",
                ["TargetType"] = "ip"
            });
        }

        [Fact]
        public void EcsStack_ShouldCreateServiceDiscoveryService()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::ServiceDiscovery::PrivateDnsNamespace", new Dictionary<string, object>
            {
                ["Name"] = "test-project.local"
            });
        }

        [Fact]
        public void EcsStack_ShouldEnableExecuteCommand()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::ECS::Service", new Dictionary<string, object>
            {
                ["EnableExecuteCommand"] = true
            });
        }

        [Fact]
        public void EcsStack_ShouldCreateAutoScalingTarget()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::ApplicationAutoScaling::ScalableTarget", new Dictionary<string, object>
            {
                ["MinCapacity"] = 2,
                ["MaxCapacity"] = 10
            });
        }

        [Fact]
        public void EcsStack_ShouldCreateCpuScalingPolicy()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::ApplicationAutoScaling::ScalingPolicy", new Dictionary<string, object>
            {
                ["PolicyType"] = "TargetTrackingScaling",
                ["TargetTrackingScalingPolicyConfiguration"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["TargetValue"] = 70
                })
            });
        }

        [Fact]
        public void EcsStack_ShouldCreateOutputs()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);
            var template = Template.FromStack(stack);

            template.HasOutput("ClusterName", new Dictionary<string, object>());
            template.HasOutput("ClusterArn", new Dictionary<string, object>());
            template.HasOutput("ServiceName", new Dictionary<string, object>());
            template.HasOutput("ServiceArn", new Dictionary<string, object>());
            template.HasOutput("ServiceNamespaceId", new Dictionary<string, object>());
            template.HasOutput("ServiceNamespaceName", new Dictionary<string, object>());
        }

        [Fact]
        public void EcsStack_ShouldHaveCorrectResourceCount()
        {
            var stack = new EcsStack(_app, "TestEcsStack", new StackProps(), _testConfig, _vpc, _securityGroup);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::ECS::Cluster", 1);
            template.ResourceCountIs("AWS::ECS::Service", 1);
            template.ResourceCountIs("AWS::ECS::TaskDefinition", 1);
            template.ResourceCountIs("AWS::ServiceDiscovery::PrivateDnsNamespace", 1);
        }
    }
}
