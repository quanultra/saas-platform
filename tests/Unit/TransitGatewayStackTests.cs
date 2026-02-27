using Amazon.CDK;
using Amazon.CDK.Assertions;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Stacks;
using AwsSapC02Practice.Infrastructure.Constructs.Network;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace AwsSapC02Practice.Tests.Unit
{
    /// <summary>
    /// Unit tests for Transit Gateway Stack
    /// Tests Requirements 6.3, 6.4
    /// </summary>
    public class TransitGatewayStackTests
    {
        private readonly StackConfiguration _testConfig;

        public TransitGatewayStackTests()
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
        public void TransitGatewayStack_ShouldCreateTransitGateway()
        {
            var app = new App();
            var stack = new TransitGatewayStack(
                app,
                "TestTgwStack",
                new StackProps(),
                _testConfig
            );
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::EC2::TransitGateway", 1);
            template.HasResourceProperties("AWS::EC2::TransitGateway", new Dictionary<string, object>
            {
                ["AmazonSideAsn"] = 64512,
                ["DnsSupport"] = "enable",
                ["DefaultRouteTableAssociation"] = "disable",
                ["DefaultRouteTablePropagation"] = "disable"
            });
        }

        [Fact]
        public void TransitGatewayStack_ShouldCreateRouteTable()
        {
            var app = new App();
            var stack = new TransitGatewayStack(
                app,
                "TestTgwStack",
                new StackProps(),
                _testConfig
            );
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::EC2::TransitGatewayRouteTable", 1);
        }

        [Fact]
        public void TransitGatewayStack_ShouldCreateOutputs()
        {
            var app = new App();
            var stack = new TransitGatewayStack(
                app,
                "TestTgwStack",
                new StackProps(),
                _testConfig
            );
            var template = Template.FromStack(stack);

            template.HasOutput("TransitGatewayId", new Dictionary<string, object>());
            template.HasOutput("TransitGatewayRouteTableId", new Dictionary<string, object>());
        }

        [Fact]
        public void TransitGatewayStack_ShouldHaveTransitGatewayConstruct()
        {
            var app = new App();
            var stack = new TransitGatewayStack(
                app,
                "TestTgwStack",
                new StackProps(),
                _testConfig
            );

            stack.TransitGateway.Should().NotBeNull();
            stack.TransitGateway.TransitGateway.Should().NotBeNull();
            stack.TransitGateway.RouteTable.Should().NotBeNull();
        }

        [Fact]
        public void TransitGatewayStack_ShouldAttachVpc()
        {
            var app = new App();
            var tgwStack = new TransitGatewayStack(
                app,
                "TestTgwStack",
                new StackProps(),
                _testConfig
            );

            // Create a VPC to attach
            var vpcStack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);

            // Attach VPC to Transit Gateway
            tgwStack.AttachVpc("TestVpc", vpcStack.PrimaryVpc);

            var template = Template.FromStack(tgwStack);

            // Should create attachment
            template.ResourceCountIs("AWS::EC2::TransitGatewayAttachment", 1);

            // Should create route table association
            template.ResourceCountIs("AWS::EC2::TransitGatewayRouteTableAssociation", 1);

            // Should create route table propagation
            template.ResourceCountIs("AWS::EC2::TransitGatewayRouteTablePropagation", 1);
        }

        [Fact]
        public void TransitGatewayStack_ShouldCreateVpcRoutesToTransitGateway()
        {
            var app = new App();
            var tgwStack = new TransitGatewayStack(
                app,
                "TestTgwStack",
                new StackProps(),
                _testConfig
            );

            var vpcStack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            tgwStack.AttachVpc("TestVpc", vpcStack.PrimaryVpc);

            var template = Template.FromStack(tgwStack);

            // Should create routes in VPC route tables (2 private subnets = 2 routes)
            template.ResourceCountIs("AWS::EC2::Route", 2);
        }

        [Fact]
        public void TransitGatewayStack_ShouldCreateAttachmentOutput()
        {
            var app = new App();
            var tgwStack = new TransitGatewayStack(
                app,
                "TestTgwStack",
                new StackProps(),
                _testConfig
            );

            var vpcStack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            tgwStack.AttachVpc("TestVpc", vpcStack.PrimaryVpc);

            var template = Template.FromStack(tgwStack);

            template.HasOutput("TestVpcAttachmentId", new Dictionary<string, object>());
        }

        [Fact]
        public void TransitGatewayStack_ShouldAttachMultipleVpcs()
        {
            var app = new App();
            var tgwStack = new TransitGatewayStack(
                app,
                "TestTgwStack",
                new StackProps(),
                _testConfig
            );

            // Create two VPCs
            var vpcStack1 = new VpcStack(app, "TestVpcStack1", new StackProps(), _testConfig);
            var vpcStack2 = new VpcStack(app, "TestVpcStack2", new StackProps(), _testConfig);

            // Attach both VPCs
            tgwStack.AttachVpc("Vpc1", vpcStack1.PrimaryVpc);
            tgwStack.AttachVpc("Vpc2", vpcStack2.PrimaryVpc);

            var template = Template.FromStack(tgwStack);

            // Should create 2 attachments
            template.ResourceCountIs("AWS::EC2::TransitGatewayAttachment", 2);

            // Should create 2 associations
            template.ResourceCountIs("AWS::EC2::TransitGatewayRouteTableAssociation", 2);

            // Should create 2 propagations
            template.ResourceCountIs("AWS::EC2::TransitGatewayRouteTablePropagation", 2);
        }
    }
}
