using Amazon.CDK;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Stacks;
using Constructs;
using FluentAssertions;
using Xunit;

namespace AwsSapC02Practice.Tests.Unit
{
    public class BaseStackTests
    {
        private class TestStack : BaseStack
        {
            public TestStack(Construct scope, string id, IStackProps props, StackConfiguration config)
                : base(scope, id, props, config)
            {
            }

            public string TestGenerateResourceName(string resourceType)
            {
                return GenerateResourceName(resourceType);
            }

            public void TestCreateOutput(string id, string value, string? description = null)
            {
                CreateOutput(id, value, description);
            }
        }

        [Fact]
        public void BaseStack_ShouldInitializeWithDefaultConfiguration()
        {
            var app = new App();
            var config = new StackConfiguration
            {
                Environment = "test",
                ProjectName = "test-project"
            };

            var stack = new TestStack(app, "TestStack", new StackProps(), config);

            stack.Should().NotBeNull();
        }

        [Fact]
        public void GenerateResourceName_ShouldFollowNamingConvention()
        {
            var app = new App();
            var config = new StackConfiguration
            {
                Environment = "dev",
                ProjectName = "my-project"
            };
            var stack = new TestStack(app, "TestStack", new StackProps(), config);

            var resourceName = stack.TestGenerateResourceName("vpc");

            resourceName.Should().Be("my-project-dev-vpc");
        }
    }
}
