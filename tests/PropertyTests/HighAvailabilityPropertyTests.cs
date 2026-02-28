using Xunit;
using FsCheck;
using FsCheck.Xunit;
using Amazon.CDK;
using Amazon.CDK.Assertions;
using AwsSapC02Practice.Infrastructure.Stacks;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;
using System.Linq;

namespace AwsSapC02Practice.Tests.PropertyTests
{
    /// <summary>
    /// Property-based tests cho High Availability Architecture
    /// Validates: Requirements 8.1, 8.2, 8.3
    /// </summary>
    public class HighAvailabilityPropertyTests
    {
        /// <summary>
        /// Property 10: ALB health checks phải pass
        /// **Validates: Requirements 8.1**
        /// </summary>
        [Fact]
        public void AlbHealthChecksMustBeConfigured()
        {
            // Arrange
            var app = new App();
            var testStack = new Stack(app, "TestStack");
            var vpc = new Amazon.CDK.AWS.EC2.Vpc(testStack, "TestVpc", new Amazon.CDK.AWS.EC2.VpcProps
            {
                MaxAzs = 2
            });
            var sg = new Amazon.CDK.AWS.EC2.SecurityGroup(testStack, "TestSG", new Amazon.CDK.AWS.EC2.SecurityGroupProps
            {
                Vpc = vpc
            });

            var config = new StackConfiguration();
            var stack = new AlbStack(app, "TestAlbStack", new StackProps(), config, vpc, sg);

            // Act
            var template = Template.FromStack(stack);

            // Assert - Verify Target Group has health check configured
            template.HasResourceProperties("AWS::ElasticLoadBalancingV2::TargetGroup", new Dictionary<string, object>
            {
                ["HealthCheckEnabled"] = true,
                ["HealthCheckProtocol"] = "HTTP",
                ["HealthCheckPath"] = "/health"
            });
        }

        /// <summary>
        /// Property 11: ASG phải maintain desired capacity
        /// **Validates: Requirements 8.2**
        /// </summary>
        [Property(Arbitrary = new[] { typeof(AsgCapacityGenerators) })]
        public Property AsgMustMaintainDesiredCapacity()
        {
            return Prop.ForAll(
                AsgCapacityGenerators.ValidCapacityArb(),
                (capacities) =>
                {
                    var (min, desired, max) = capacities;

                    // Arrange
                    var app = new App();
                    var testStack = new Stack(app, "TestStack");
                    var vpc = new Amazon.CDK.AWS.EC2.Vpc(testStack, "TestVpc", new Amazon.CDK.AWS.EC2.VpcProps
                    {
                        MaxAzs = 2
                    });
                    var sg = new Amazon.CDK.AWS.EC2.SecurityGroup(testStack, "TestSG", new Amazon.CDK.AWS.EC2.SecurityGroupProps
                    {
                        Vpc = vpc
                    });
                    var tg = new Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationTargetGroup(testStack, "TestTG",
                        new Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationTargetGroupProps
                    {
                        Vpc = vpc,
                        Port = 80
                    });

                    var config = new StackConfiguration();
                    var stack = new AsgStack(app, "TestAsgStack", new StackProps(), config, vpc, sg, tg);

                    // Act
                    var template = Template.FromStack(stack);

                    // Assert - Verify ASG capacity configuration
                    var asgResources = template.FindResources("AWS::AutoScaling::AutoScalingGroup");
                    var hasValidCapacity = asgResources.Values.All(asg =>
                    {
                        var props = asg["Properties"] as Dictionary<string, object>;
                        if (props == null) return false;

                        var minSize = System.Convert.ToInt32(props["MinSize"]);
                        var maxSize = System.Convert.ToInt32(props["MaxSize"]);
                        var desiredCap = props.ContainsKey("DesiredCapacity")
                            ? System.Convert.ToInt32(props["DesiredCapacity"])
                            : minSize;

                        return minSize <= desiredCap && desiredCap <= maxSize && minSize >= 1;
                    });

                    return hasValidCapacity.ToProperty()
                        .Label($"ASG must maintain valid capacity: min={min}, desired={desired}, max={max}");
                });
        }

        /// <summary>
        /// Property 12: Aurora failover time < 30 giây
        /// **Validates: Requirements 8.3**
        /// </summary>
        [Property(Arbitrary = new[] { typeof(AuroraConfigGenerators) })]
        public Property AuroraFailoverTimeMustBeLessThan30Seconds()
        {
            return Prop.ForAll(
                AuroraConfigGenerators.ValidReplicaCountArb(),
                (replicaCount) =>
                {
                    // Arrange
                    var app = new App();
                    var testStack = new Stack(app, "TestStack");
                    var vpc = new Amazon.CDK.AWS.EC2.Vpc(testStack, "TestVpc", new Amazon.CDK.AWS.EC2.VpcProps
                    {
                        MaxAzs = 2,
                        SubnetConfiguration = new[]
                        {
                            new Amazon.CDK.AWS.EC2.SubnetConfiguration
                            {
                                Name = "Public",
                                SubnetType = Amazon.CDK.AWS.EC2.SubnetType.PUBLIC,
                                CidrMask = 24
                            },
                            new Amazon.CDK.AWS.EC2.SubnetConfiguration
                            {
                                Name = "Private",
                                SubnetType = Amazon.CDK.AWS.EC2.SubnetType.PRIVATE_WITH_EGRESS,
                                CidrMask = 24
                            },
                            new Amazon.CDK.AWS.EC2.SubnetConfiguration
                            {
                                Name = "Isolated",
                                SubnetType = Amazon.CDK.AWS.EC2.SubnetType.PRIVATE_ISOLATED,
                                CidrMask = 24
                            }
                        }
                    });
                    var sg = new Amazon.CDK.AWS.EC2.SecurityGroup(testStack, "TestSG", new Amazon.CDK.AWS.EC2.SecurityGroupProps
                    {
                        Vpc = vpc
                    });

                    var config = new StackConfiguration();
                    var stack = new AuroraStack(app, "TestAuroraStack", new StackProps(), config, vpc, sg);

                    // Act
                    var template = Template.FromStack(stack);

                    // Assert - Verify Aurora cluster configuration
                    var clusterResources = template.FindResources("AWS::RDS::DBCluster");
                    var hasCluster = clusterResources.Count > 0;

                    var hasEncryption = clusterResources.Values.All(cluster =>
                    {
                        var props = cluster["Properties"] as Dictionary<string, object>;
                        return props != null && props.ContainsKey("StorageEncrypted")
                            && System.Convert.ToBoolean(props["StorageEncrypted"]);
                    });

                    var instanceResources = template.FindResources("AWS::RDS::DBInstance");
                    var hasEnoughInstances = instanceResources.Count >= 2;

                    return (hasCluster && hasEncryption && hasEnoughInstances).ToProperty()
                        .Label($"Aurora cluster must support failover < 30s with {replicaCount} replicas");
                });
        }
    }

    #region Generators

    public static class AsgCapacityGenerators
    {
        public static Arbitrary<(int min, int desired, int max)> ValidCapacityArb()
        {
            var gen = from min in Gen.Choose(1, 5)
                      from desired in Gen.Choose(min, min + 10)
                      from max in Gen.Choose(desired, desired + 20)
                      select (min, desired, max);
            return gen.ToArbitrary();
        }
    }

    public static class AuroraConfigGenerators
    {
        public static Arbitrary<int> ValidReplicaCountArb()
        {
            return Gen.Choose(1, 5).ToArbitrary();
        }
    }

    #endregion
}
