using Amazon.CDK;
using Amazon.CDK.Assertions;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Stacks;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace AwsSapC02Practice.Tests.Unit
{
    public class VpcStackTests
    {
        private readonly StackConfiguration _testConfig;

        public VpcStackTests()
        {
            _testConfig = new StackConfiguration
            {
                Environment = "test",
                ProjectName = "test-project",
                Network = new NetworkConfiguration
                {
                    PrimaryCidr = "10.0.0.0/16",
                    SecondaryCidr = "10.1.0.0/16",
                    MaxAzs = 3,
                    EnableNatGateway = true
                },
                MultiRegion = new MultiRegionConfig
                {
                    PrimaryRegion = "us-east-1",
                    SecondaryRegion = "eu-west-1"
                }
            };
        }

        [Fact]
        public void VpcStack_ShouldCreatePrimaryVpc()
        {
            var app = new App();
            var stack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);

            stack.PrimaryVpc.Should().NotBeNull();
            stack.PrimaryVpc.Vpc.Should().NotBeNull();
        }

        [Fact]
        public void VpcStack_ShouldCreateSecurityGroups()
        {
            var app = new App();
            var stack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);

            stack.PrimaryVpc.ApplicationSecurityGroup.Should().NotBeNull();
            stack.PrimaryVpc.DatabaseSecurityGroup.Should().NotBeNull();
            stack.PrimaryVpc.LoadBalancerSecurityGroup.Should().NotBeNull();
        }

        [Fact]
        public void VpcStack_ShouldCreateVpcWithCorrectCidr()
        {
            var app = new App();
            var stack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::EC2::VPC", new Dictionary<string, object>
            {
                ["CidrBlock"] = "10.0.0.0/16"
            });
        }

        [Fact]
        public void VpcStack_ShouldCreateSubnets()
        {
            var app = new App();
            var stack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            // 3 AZs * 2 subnet types (Public and Private) = 6 subnets
            // Isolated subnets are not created by default in CDK VPC
            template.ResourceCountIs("AWS::EC2::Subnet", 6);
        }

        [Fact]
        public void VpcStack_ShouldCreateNatGateways()
        {
            var app = new App();
            var stack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::EC2::NatGateway", 2);
        }

        [Fact]
        public void VpcStack_ShouldCreateInternetGateway()
        {
            var app = new App();
            var stack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::EC2::InternetGateway", 1);
        }

        [Fact]
        public void VpcStack_ShouldCreateThreeSecurityGroups()
        {
            var app = new App();
            var stack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::EC2::SecurityGroup", 3);
        }

        [Fact]
        public void VpcStack_ShouldCreateOutputsForPrimaryVpc()
        {
            var app = new App();
            var stack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            template.HasOutput("PrimaryVpcId", new Dictionary<string, object>());
            template.HasOutput("PrimaryVpcCidr", new Dictionary<string, object>());
            template.HasOutput("PrimaryAppSecurityGroupId", new Dictionary<string, object>());
            template.HasOutput("PrimaryDbSecurityGroupId", new Dictionary<string, object>());
            template.HasOutput("PrimaryLbSecurityGroupId", new Dictionary<string, object>());
        }

        [Fact]
        public void VpcStack_ShouldEnableDnsSupport()
        {
            var app = new App();
            var stack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::EC2::VPC", new Dictionary<string, object>
            {
                ["EnableDnsSupport"] = true,
                ["EnableDnsHostnames"] = true
            });
        }

        [Fact]
        public void VpcStack_SecondaryVpcShouldBeNullInitially()
        {
            var app = new App();
            var stack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);

            stack.SecondaryVpc.Should().BeNull();
        }

        [Fact]
        public void VpcStack_ShouldCreateSecondaryVpc()
        {
            var app = new App();
            var stack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);

            // Secondary VPC must be created within the stack scope
            stack.CreateSecondaryVpc(stack, _testConfig);

            stack.SecondaryVpc.Should().NotBeNull();
            stack.SecondaryVpc!.Vpc.Should().NotBeNull();
        }

        [Fact]
        public void VpcStack_WithoutNatGateway_ShouldNotCreateNatGateways()
        {
            var app = new App();
            var config = new StackConfiguration
            {
                Environment = "test",
                ProjectName = "test-project",
                Network = new NetworkConfiguration
                {
                    PrimaryCidr = "10.0.0.0/16",
                    MaxAzs = 3,
                    EnableNatGateway = false
                },
                MultiRegion = new MultiRegionConfig
                {
                    PrimaryRegion = "us-east-1",
                    SecondaryRegion = "eu-west-1"
                }
            };

            var stack = new VpcStack(app, "TestVpcStack", new StackProps(), config);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::EC2::NatGateway", 0);
        }
    }
}
