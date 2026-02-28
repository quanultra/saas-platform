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
    /// Unit tests for VPN Stack
    /// Tests Requirements 6.1, 6.2
    /// </summary>
    public class VpnStackTests
    {
        private readonly StackConfiguration _testConfig;

        public VpnStackTests()
        {
            _testConfig = new StackConfiguration
            {
                Environment = "test",
                ProjectName = "test-project",
                Network = new NetworkConfiguration
                {
                    PrimaryCidr = "10.0.0.0/16",
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
        public void VpnStack_ShouldCreateCustomerGateway()
        {
            var app = new App();
            var vpcStack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var stack = new VpnStack(
                app,
                "TestVpnStack",
                new StackProps(),
                _testConfig,
                vpcStack.PrimaryVpc,
                "203.0.113.1"
            );
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::EC2::CustomerGateway", 1);
            template.HasResourceProperties("AWS::EC2::CustomerGateway", new Dictionary<string, object>
            {
                ["Type"] = "ipsec.1",
                ["IpAddress"] = "203.0.113.1",
                ["BgpAsn"] = 65000
            });
        }

        [Fact]
        public void VpnStack_ShouldCreateVirtualPrivateGateway()
        {
            var app = new App();
            var vpcStack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var stack = new VpnStack(
                app,
                "TestVpnStack",
                new StackProps(),
                _testConfig,
                vpcStack.PrimaryVpc
            );
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::EC2::VPNGateway", 1);
            template.HasResourceProperties("AWS::EC2::VPNGateway", new Dictionary<string, object>
            {
                ["Type"] = "ipsec.1",
                ["AmazonSideAsn"] = 64512
            });
        }

        [Fact]
        public void VpnStack_ShouldAttachVpnGatewayToVpc()
        {
            var app = new App();
            var vpcStack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var stack = new VpnStack(
                app,
                "TestVpnStack",
                new StackProps(),
                _testConfig,
                vpcStack.PrimaryVpc
            );
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::EC2::VPCGatewayAttachment", 1);
        }

        [Fact]
        public void VpnStack_ShouldCreateVpnConnection()
        {
            var app = new App();
            var vpcStack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var stack = new VpnStack(
                app,
                "TestVpnStack",
                new StackProps(),
                _testConfig,
                vpcStack.PrimaryVpc
            );
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::EC2::VPNConnection", 1);
            template.HasResourceProperties("AWS::EC2::VPNConnection", new Dictionary<string, object>
            {
                ["Type"] = "ipsec.1",
                ["StaticRoutesOnly"] = false // BGP enabled
            });
        }

        [Fact]
        public void VpnStack_ShouldCreateOutputs()
        {
            var app = new App();
            var vpcStack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var stack = new VpnStack(
                app,
                "TestVpnStack",
                new StackProps(),
                _testConfig,
                vpcStack.PrimaryVpc
            );
            var template = Template.FromStack(stack);

            template.HasOutput("CustomerGatewayId", new Dictionary<string, object>());
            template.HasOutput("VirtualPrivateGatewayId", new Dictionary<string, object>());
            template.HasOutput("VpnConnectionId", new Dictionary<string, object>());
        }

        [Fact]
        public void VpnStack_ShouldHaveSiteToSiteVpnConstruct()
        {
            var app = new App();
            var vpcStack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var stack = new VpnStack(
                app,
                "TestVpnStack",
                new StackProps(),
                _testConfig,
                vpcStack.PrimaryVpc
            );

            stack.SiteToSiteVpn.Should().NotBeNull();
            stack.SiteToSiteVpn.CustomerGateway.Should().NotBeNull();
            stack.SiteToSiteVpn.VirtualPrivateGateway.Should().NotBeNull();
            stack.SiteToSiteVpn.VpnConnection.Should().NotBeNull();
        }

        [Fact]
        public void VpnStack_ShouldEnableRoutePropagation()
        {
            var app = new App();
            var vpcStack = new VpcStack(app, "TestVpcStack", new StackProps(), _testConfig);
            var stack = new VpnStack(
                app,
                "TestVpnStack",
                new StackProps(),
                _testConfig,
                vpcStack.PrimaryVpc
            );
            var template = Template.FromStack(stack);

            // Should have route propagation for private subnets
            template.ResourceCountIs("AWS::EC2::VPNGatewayRoutePropagation", 2); // 2 private subnets
        }
    }
}
