using Amazon.CDK;
using Amazon.CDK.Assertions;
using AwsSapC02Practice.Infrastructure.Stacks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using System.Collections.Generic;
using System.Linq;

namespace AwsSapC02Practice.Tests.PropertyTests
{
    public class SecurityPropertyTests
    {
        [Property(MaxTest = 10)]
        public Property AllS3BucketsShouldBeEncrypted()
        {
            var regionGen = Gen.Elements("us-east-1", "us-west-2");
            return Prop.ForAll(regionGen.ToArbitrary(), (region) =>
            {
                var app = new App();
                var stack = new CloudTrailStack(app, "Test", new StackProps
                {
                    Env = new Amazon.CDK.Environment { Region = region, Account = "123456789012" }
                });
                var template = Template.FromStack(stack);
                var buckets = template.FindResources("AWS::S3::Bucket");
                buckets.Should().NotBeEmpty();
                return true;
            });
        }

        [Property(MaxTest = 10)]
        public Property KmsKeysShouldHaveRotation()
        {
            var regionGen = Gen.Elements("us-east-1", "us-west-2");
            return Prop.ForAll(regionGen.ToArbitrary(), (region) =>
            {
                var app = new App();
                var stack = new KmsStack(app, "Test", new StackProps
                {
                    Env = new Amazon.CDK.Environment { Region = region, Account = "123456789012" }
                });
                var template = Template.FromStack(stack);
                var keys = template.FindResources("AWS::KMS::Key");
                keys.Should().NotBeEmpty();
                return true;
            });
        }

        [Property(MaxTest = 10)]
        public Property CloudTrailShouldBeMultiRegion()
        {
            var regionGen = Gen.Elements("us-east-1", "us-west-2");
            return Prop.ForAll(regionGen.ToArbitrary(), (region) =>
            {
                var app = new App();
                var stack = new CloudTrailStack(app, "Test", new StackProps
                {
                    Env = new Amazon.CDK.Environment { Region = region, Account = "123456789012" }
                });
                var template = Template.FromStack(stack);
                template.ResourceCountIs("AWS::CloudTrail::Trail", 1);
                template.HasResourceProperties("AWS::CloudTrail::Trail", Match.ObjectLike(new Dictionary<string, object>
                {
                    ["IsMultiRegionTrail"] = true,
                    ["EnableLogFileValidation"] = true
                }));
                return true;
            });
        }
    }
}
