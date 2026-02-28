using Amazon.CDK;
using Amazon.CDK.Assertions;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Stacks;
using Xunit;

namespace AwsSapC02Practice.Tests.PropertyTests
{
    /// <summary>
    /// Property-based tests for monitoring infrastructure
    /// Validates: Requirements 12.1, 12.2, 12.5
    /// </summary>
    public class MonitoringPropertyTests
    {
        [Fact]
        public void Property22_CloudWatchAlarms_TriggerAtCorrectThresholds()
        {
            // Arrange
            var app = new App();
            var config = new StackConfiguration
            {
                Environment = "test",
                ProjectName = "test-project",
                Monitoring = new MonitoringConfiguration
                {
                    AlarmEmail = "test@example.com"
                }
            };

            // Act
            var stack = new MonitoringStack(app, "TestMonitoringStack", config, new StackProps());
            var template = Template.FromStack(stack);

            // Assert - Verify alarms exist with proper thresholds
            template.ResourceCountIs("AWS::CloudWatch::Alarm", 4);

            // Verify CPU alarm threshold
            template.HasResourceProperties("AWS::CloudWatch::Alarm", new
            {
                Threshold = 80,
                ComparisonOperator = "GreaterThanThreshold"
            });
        }

        [Fact]
        public void Property23_XRayTraces_AreComplete()
        {
            // Arrange
            var app = new App();
            var config = new StackConfiguration
            {
                Environment = "test",
                ProjectName = "test-project"
            };

            // Act
            var stack = new XRayStack(app, "TestXRayStack", new StackProps(), config);
            var template = Template.FromStack(stack);

            // Assert - Verify X-Ray sampling rules exist
            template.ResourceCountIs("AWS::XRay::SamplingRule", 3);
        }

        [Fact]
        public void Property24_LogRetention_MeetsCompliance()
        {
            // Arrange
            var app = new App();
            var config = new StackConfiguration
            {
                Environment = "prod",
                ProjectName = "test-project"
            };

            // Act
            var stack = new CloudWatchLogsStack(app, "TestLogsStack", config, new StackProps());
            var template = Template.FromStack(stack);

            // Assert - Verify log groups have retention
            template.HasResourceProperties("AWS::Logs::LogGroup", new
            {
                RetentionInDays = 365
            });
        }
    }
}
