using Amazon.CDK;
using Amazon.CDK.Assertions;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Stacks;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace AwsSapC02Practice.Tests.Unit
{
    public class S3StackTests
    {
        private readonly StackConfiguration _testConfig;

        public S3StackTests()
        {
            _testConfig = new StackConfiguration
            {
                Environment = "test",
                ProjectName = "test-project",
                MultiRegion = new MultiRegionConfig
                {
                    PrimaryRegion = "us-east-1",
                    SecondaryRegion = "eu-west-1",
                    EnableCrossRegionReplication = true
                }
            };
        }

        [Fact]
        public void S3Stack_ShouldCreateCrossRegionStorage()
        {
            var app = new App();
            var stack = new S3Stack(app, "TestS3Stack", new StackProps(), _testConfig);

            stack.CrossRegionStorage.Should().NotBeNull();
            stack.CrossRegionStorage.PrimaryBucket.Should().NotBeNull();
            stack.CrossRegionStorage.SecondaryBucket.Should().NotBeNull();
            stack.CrossRegionStorage.ReplicationRole.Should().NotBeNull();
        }

        [Fact]
        public void S3Stack_ShouldCreateTwoBuckets()
        {
            var app = new App();
            var stack = new S3Stack(app, "TestS3Stack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::S3::Bucket", 2);
        }

        [Fact]
        public void S3Stack_PrimaryBucket_ShouldHaveVersioningEnabled()
        {
            var app = new App();
            var stack = new S3Stack(app, "TestS3Stack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
            {
                ["VersioningConfiguration"] = new Dictionary<string, object>
                {
                    ["Status"] = "Enabled"
                }
            });
        }

        [Fact]
        public void S3Stack_PrimaryBucket_ShouldHaveEncryption()
        {
            var app = new App();
            var stack = new S3Stack(app, "TestS3Stack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
            {
                ["BucketEncryption"] = new Dictionary<string, object>
                {
                    ["ServerSideEncryptionConfiguration"] = Match.AnyValue()
                }
            });
        }

        [Fact]
        public void S3Stack_PrimaryBucket_ShouldBlockPublicAccess()
        {
            var app = new App();
            var stack = new S3Stack(app, "TestS3Stack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
            {
                ["PublicAccessBlockConfiguration"] = new Dictionary<string, object>
                {
                    ["BlockPublicAcls"] = true,
                    ["BlockPublicPolicy"] = true,
                    ["IgnorePublicAcls"] = true,
                    ["RestrictPublicBuckets"] = true
                }
            });
        }

        [Fact]
        public void S3Stack_PrimaryBucket_ShouldHaveLifecycleRules()
        {
            var app = new App();
            var stack = new S3Stack(app, "TestS3Stack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
            {
                ["LifecycleConfiguration"] = new Dictionary<string, object>
                {
                    ["Rules"] = Match.AnyValue()
                }
            });
        }

        [Fact]
        public void S3Stack_PrimaryBucket_ShouldHaveReplicationConfiguration()
        {
            var app = new App();
            var stack = new S3Stack(app, "TestS3Stack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
            {
                ["ReplicationConfiguration"] = new Dictionary<string, object>
                {
                    ["Role"] = Match.AnyValue(),
                    ["Rules"] = Match.AnyValue()
                }
            });
        }

        [Fact]
        public void S3Stack_ShouldCreateOutputs()
        {
            var app = new App();
            var stack = new S3Stack(app, "TestS3Stack", new StackProps(), _testConfig);
            var template = Template.FromStack(stack);

            template.HasOutput("PrimaryBucketName", new Dictionary<string, object>());
            template.HasOutput("PrimaryBucketArn", new Dictionary<string, object>());
            template.HasOutput("SecondaryBucketName", new Dictionary<string, object>());
            template.HasOutput("SecondaryBucketArn", new Dictionary<string, object>());
            template.HasOutput("ReplicationRoleArn", new Dictionary<string, object>());
        }
    }
}
