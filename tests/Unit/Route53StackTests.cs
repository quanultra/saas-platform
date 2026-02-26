using Amazon.CDK;
using Amazon.CDK.Assertions;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Stacks;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace AwsSapC02Practice.Tests.Unit
{
    public class Route53StackTests
    {
        private readonly StackConfiguration _testConfig;
        private readonly Route53StackProps _route53Props;

        public Route53StackTests()
        {
            _testConfig = new StackConfiguration
            {
                Environment = "test",
                ProjectName = "test-project",
                MultiRegion = new MultiRegionConfig
                {
                    PrimaryRegion = "us-east-1",
                    SecondaryRegion = "eu-west-1"
                }
            };

            _route53Props = new Route53StackProps
            {
                DomainName = "example.com",
                PrimaryEndpoint = "primary-alb-123456.us-east-1.elb.amazonaws.com",
                SecondaryEndpoint = "secondary-alb-789012.eu-west-1.elb.amazonaws.com",
                PrimaryRegion = "us-east-1",
                SecondaryRegion = "eu-west-1"
            };
        }

        [Fact]
        public void Route53Stack_ShouldCreateHostedZone()
        {
            var app = new App();
            var stack = new Route53Stack(app, "TestRoute53Stack", new StackProps(), _testConfig, _route53Props);

            stack.HostedZone.Should().NotBeNull();
            stack.HostedZone.ZoneName.Should().Be("example.com");
        }

        [Fact]
        public void Route53Stack_ShouldCreatePrimaryHealthCheck()
        {
            var app = new App();
            var stack = new Route53Stack(app, "TestRoute53Stack", new StackProps(), _testConfig, _route53Props);

            stack.PrimaryHealthCheck.Should().NotBeNull();
        }

        [Fact]
        public void Route53Stack_ShouldCreateSecondaryHealthCheck()
        {
            var app = new App();
            var stack = new Route53Stack(app, "TestRoute53Stack", new StackProps(), _testConfig, _route53Props);

            stack.SecondaryHealthCheck.Should().NotBeNull();
        }

        [Fact]
        public void Route53Stack_ShouldCreateHostedZoneResource()
        {
            var app = new App();
            var stack = new Route53Stack(app, "TestRoute53Stack", new StackProps(), _testConfig, _route53Props);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::Route53::HostedZone", 1);
        }

        [Fact]
        public void Route53Stack_ShouldCreateTwoHealthChecks()
        {
            var app = new App();
            var stack = new Route53Stack(app, "TestRoute53Stack", new StackProps(), _testConfig, _route53Props);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::Route53::HealthCheck", 2);
        }

        [Fact]
        public void Route53Stack_ShouldCreateTwoRecordSets()
        {
            var app = new App();
            var stack = new Route53Stack(app, "TestRoute53Stack", new StackProps(), _testConfig, _route53Props);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::Route53::RecordSet", 2);
        }

        [Fact]
        public void Route53Stack_ShouldCreateTwoCloudWatchAlarms()
        {
            var app = new App();
            var stack = new Route53Stack(app, "TestRoute53Stack", new StackProps(), _testConfig, _route53Props);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::CloudWatch::Alarm", 2);
        }

        [Fact]
        public void Route53Stack_ShouldCreateRequiredOutputs()
        {
            var app = new App();
            var stack = new Route53Stack(app, "TestRoute53Stack", new StackProps(), _testConfig, _route53Props);
            var template = Template.FromStack(stack);

            template.HasOutput("HostedZoneId", new Dictionary<string, object>());
            template.HasOutput("HostedZoneName", new Dictionary<string, object>());
            template.HasOutput("PrimaryHealthCheckId", new Dictionary<string, object>());
            template.HasOutput("SecondaryHealthCheckId", new Dictionary<string, object>());
            template.HasOutput("NameServers", new Dictionary<string, object>());
        }
    }
}
