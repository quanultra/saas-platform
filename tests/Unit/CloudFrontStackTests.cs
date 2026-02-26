using Amazon.CDK;
using Amazon.CDK.Assertions;
using Amazon.CDK.AWS.S3;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Stacks;
using Constructs;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace AwsSapC02Practice.Tests.Unit
{
    public class CloudFrontStackTests
    {
        private readonly StackConfiguration _testConfig;

        public CloudFrontStackTests()
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
        }

        [Fact]
        public void CloudFrontStack_ShouldCreateDistribution()
        {
            var app = new App();
            var tempStack = new Stack(app, "TempStack");
            var originBucket = Bucket.FromBucketAttributes(tempStack, "OriginBucket", new BucketAttributes
            {
                BucketName = "test-origin-bucket",
                BucketRegionalDomainName = "test-origin-bucket.s3.us-east-1.amazonaws.com"
            });

            var stack = new CloudFrontStack(
                app,
                "TestCloudFrontStack",
                new StackProps(),
                _testConfig,
                originBucket);

            stack.Distribution.Should().NotBeNull();
        }

        [Fact]
        public void CloudFrontStack_ShouldCreateLogBucket()
        {
            var app = new App();
            var tempStack = new Stack(app, "TempStack");
            var originBucket = Bucket.FromBucketAttributes(tempStack, "OriginBucket", new BucketAttributes
            {
                BucketName = "test-origin-bucket",
                BucketRegionalDomainName = "test-origin-bucket.s3.us-east-1.amazonaws.com"
            });

            var stack = new CloudFrontStack(
                app,
                "TestCloudFrontStack",
                new StackProps(),
                _testConfig,
                originBucket);

            stack.LogBucket.Should().NotBeNull();
        }


        [Fact]
        public void CloudFrontStack_ShouldCreateCloudFrontDistribution()
        {
            var app = new App();
            var tempStack = new Stack(app, "TempStack");
            var originBucket = Bucket.FromBucketAttributes(tempStack, "OriginBucket", new BucketAttributes
            {
                BucketName = "test-origin-bucket",
                BucketRegionalDomainName = "test-origin-bucket.s3.us-east-1.amazonaws.com"
            });

            var stack = new CloudFrontStack(
                app,
                "TestCloudFrontStack",
                new StackProps(),
                _testConfig,
                originBucket);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::CloudFront::Distribution", 1);
        }

        [Fact]
        public void CloudFrontStack_ShouldCreateLogBucketResource()
        {
            var app = new App();
            var tempStack = new Stack(app, "TempStack");
            var originBucket = Bucket.FromBucketAttributes(tempStack, "OriginBucket", new BucketAttributes
            {
                BucketName = "test-origin-bucket",
                BucketRegionalDomainName = "test-origin-bucket.s3.us-east-1.amazonaws.com"
            });

            var stack = new CloudFrontStack(
                app,
                "TestCloudFrontStack",
                new StackProps(),
                _testConfig,
                originBucket);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::S3::Bucket", 1);
        }


        [Fact]
        public void CloudFrontStack_Distribution_ShouldEnableHttps()
        {
            var app = new App();
            var tempStack = new Stack(app, "TempStack");
            var originBucket = Bucket.FromBucketAttributes(tempStack, "OriginBucket", new BucketAttributes
            {
                BucketName = "test-origin-bucket",
                BucketRegionalDomainName = "test-origin-bucket.s3.us-east-1.amazonaws.com"
            });

            var stack = new CloudFrontStack(
                app,
                "TestCloudFrontStack",
                new StackProps(),
                _testConfig,
                originBucket);
            var template = Template.FromStack(stack);

            var distConfig = new Dictionary<string, object>
            {
                ["DistributionConfig"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["DefaultCacheBehavior"] = Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["ViewerProtocolPolicy"] = "redirect-to-https"
                    })
                })
            };

            template.HasResourceProperties("AWS::CloudFront::Distribution", distConfig);
        }

        [Fact]
        public void CloudFrontStack_Distribution_ShouldEnableCompression()
        {
            var app = new App();
            var tempStack = new Stack(app, "TempStack");
            var originBucket = Bucket.FromBucketAttributes(tempStack, "OriginBucket", new BucketAttributes
            {
                BucketName = "test-origin-bucket",
                BucketRegionalDomainName = "test-origin-bucket.s3.us-east-1.amazonaws.com"
            });

            var stack = new CloudFrontStack(
                app,
                "TestCloudFrontStack",
                new StackProps(),
                _testConfig,
                originBucket);
            var template = Template.FromStack(stack);

            var distConfig = new Dictionary<string, object>
            {
                ["DistributionConfig"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["DefaultCacheBehavior"] = Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["Compress"] = true
                    })
                })
            };

            template.HasResourceProperties("AWS::CloudFront::Distribution", distConfig);
        }

        [Fact]
        public void CloudFrontStack_Distribution_ShouldEnableIPv6()
        {
            var app = new App();
            var tempStack = new Stack(app, "TempStack");
            var originBucket = Bucket.FromBucketAttributes(tempStack, "OriginBucket", new BucketAttributes
            {
                BucketName = "test-origin-bucket",
                BucketRegionalDomainName = "test-origin-bucket.s3.us-east-1.amazonaws.com"
            });

            var stack = new CloudFrontStack(
                app,
                "TestCloudFrontStack",
                new StackProps(),
                _testConfig,
                originBucket);
            var template = Template.FromStack(stack);

            var distConfig = new Dictionary<string, object>
            {
                ["DistributionConfig"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["IPV6Enabled"] = true
                })
            };

            template.HasResourceProperties("AWS::CloudFront::Distribution", distConfig);
        }


        [Fact]
        public void CloudFrontStack_ShouldCreateCachePolicy()
        {
            var app = new App();
            var tempStack = new Stack(app, "TempStack");
            var originBucket = Bucket.FromBucketAttributes(tempStack, "OriginBucket", new BucketAttributes
            {
                BucketName = "test-origin-bucket",
                BucketRegionalDomainName = "test-origin-bucket.s3.us-east-1.amazonaws.com"
            });

            var stack = new CloudFrontStack(
                app,
                "TestCloudFrontStack",
                new StackProps(),
                _testConfig,
                originBucket);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::CloudFront::CachePolicy", 1);
        }


        [Fact]
        public void CloudFrontStack_ShouldCreateResponseHeadersPolicy()
        {
            var app = new App();
            var tempStack = new Stack(app, "TempStack");
            var originBucket = Bucket.FromBucketAttributes(tempStack, "OriginBucket", new BucketAttributes
            {
                BucketName = "test-origin-bucket",
                BucketRegionalDomainName = "test-origin-bucket.s3.us-east-1.amazonaws.com"
            });

            var stack = new CloudFrontStack(
                app,
                "TestCloudFrontStack",
                new StackProps(),
                _testConfig,
                originBucket);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::CloudFront::ResponseHeadersPolicy", 1);
        }

        [Fact]
        public void CloudFrontStack_ShouldCreateOutputs()
        {
            var app = new App();
            var tempStack = new Stack(app, "TempStack");
            var originBucket = Bucket.FromBucketAttributes(tempStack, "OriginBucket", new BucketAttributes
            {
                BucketName = "test-origin-bucket",
                BucketRegionalDomainName = "test-origin-bucket.s3.us-east-1.amazonaws.com"
            });

            var stack = new CloudFrontStack(
                app,
                "TestCloudFrontStack",
                new StackProps(),
                _testConfig,
                originBucket);
            var template = Template.FromStack(stack);

            template.HasOutput("DistributionId", new Dictionary<string, object>());
            template.HasOutput("DistributionDomainName", new Dictionary<string, object>());
            template.HasOutput("LogBucketName", new Dictionary<string, object>());
        }

        [Fact]
        public void CloudFrontStack_ShouldHaveCorrectTags()
        {
            var app = new App();
            var tempStack = new Stack(app, "TempStack");
            var originBucket = Bucket.FromBucketAttributes(tempStack, "OriginBucket", new BucketAttributes
            {
                BucketName = "test-origin-bucket",
                BucketRegionalDomainName = "test-origin-bucket.s3.us-east-1.amazonaws.com"
            });

            var stack = new CloudFrontStack(
                app,
                "TestCloudFrontStack",
                new StackProps(),
                _testConfig,
                originBucket);

            var stackTags = stack.Tags.TagValues();
            stackTags.Should().ContainKey("Component");
            stackTags.Should().ContainKey("Service");
            stackTags["Component"].Should().Be("CloudFront");
            stackTags["Service"].Should().Be("CDN");
        }
    }
}
