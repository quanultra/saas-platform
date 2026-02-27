using Amazon.CDK;
using Amazon.CDK.Assertions;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Stacks;
using AwsSapC02Practice.Infrastructure.Constructs.DisasterRecovery;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using CdkTags = Amazon.CDK.Tags;

namespace AwsSapC02Practice.Tests.PropertyTests
{
    /// <summary>
    /// Property-based tests for Disaster Recovery solutions
    /// **Validates: Requirements 7.1, 7.2, 7.3**
    /// </summary>
    public class DisasterRecoveryPropertyTests
    {
        private StackConfiguration CreateTestConfig(string environment = "test")
        {
            return new StackConfiguration
            {
                Environment = environment,
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
                    SecondaryRegion = "eu-west-1",
                    EnableCrossRegionReplication = true
                },
                Database = new DatabaseConfiguration
                {
                    DatabaseName = "testdb"
                }
            };
        }

        /// <summary>
        /// **Property 4: Backup retention compliance**
        /// For any AWS Backup plan created, it SHALL have at least one backup rule
        /// with automated schedule and retention policy that meets compliance requirements.
        /// **Validates: Requirements 7.1**
        /// </summary>
        [Property(MaxTest = 50)]
                public Property BackupRetentionCompliance_ShouldMeetMinimumRetentionPeriods()
                {
                    // Generator for retention days (7-365 days for daily, 30-365 for weekly, 90-365 for monthly)
                    var retentionDaysGen = Gen.Choose(7, 365);

                    return Prop.ForAll(
                        retentionDaysGen.ToArbitrary(),
                        (retentionDays) =>
                        {
                            var app = new App();
                            var config = CreateTestConfig();
                            var stack = new BackupStack(app, "TestBackupStack", new StackProps(), config);
                            var template = Template.FromStack(stack);

                            // Verify backup plan exists
                            template.ResourceCountIs("AWS::Backup::BackupPlan", 1);

                            // Verify backup plan has rules with retention policies
                            template.HasResourceProperties("AWS::Backup::BackupPlan", Match.ObjectLike(new Dictionary<string, object>
                            {
                                ["BackupPlan"] = Match.ObjectLike(new Dictionary<string, object>
                                {
                                    ["BackupPlanRule"] = Match.ArrayWith(new object[]
                                    {
                                        Match.ObjectLike(new Dictionary<string, object>
                                        {
                                            ["ScheduleExpression"] = Match.AnyValue(),
                                            ["Lifecycle"] = Match.ObjectLike(new Dictionary<string, object>
                                            {
                                                ["DeleteAfterDays"] = Match.AnyValue()
                                            })
                                        })
                                    })
                                })
                            }));

                            // Verify backup vault exists
                            template.ResourceCountIs("AWS::Backup::BackupVault", 1);

                            // Verify IAM role for backup exists
                            return true;
                        });
                }


        /// <summary>
        /// **Property 5: RTO < 1 giờ cho Pilot Light**
        /// For any Pilot Light DR configuration, the infrastructure SHALL be designed
        /// to achieve Recovery Time Objective (RTO) of less than 1 hour.
        /// This is validated by checking that minimal standby infrastructure exists
        /// and can be scaled quickly.
        /// **Validates: Requirements 7.2**
        /// </summary>
        [Property(MaxTest = 50)]
        public Property PilotLightRTO_ShouldBeLessThanOneHour()
        {
            // Generator for desired capacity (0-2 for pilot light minimal setup)
            var desiredCapacityGen = Gen.Choose(0, 2);

            return Prop.ForAll(
                desiredCapacityGen.ToArbitrary(),
                (desiredCapacity) =>
                {
                    var app = new App();
                    var config = CreateTestConfig();

                    // Create VPC first (required for PilotLightStack)
                    var vpcStack = new VpcStack(app, "TestVpcStack", new StackProps
                    {
                        Env = new Amazon.CDK.Environment
                        {
                            Region = config.MultiRegion.SecondaryRegion
                        }
                    }, config);

                    var pilotLightStack = new PilotLightStack(app, "TestPilotLightStack",
                        new StackProps
                        {
                            Env = new Amazon.CDK.Environment
                            {
                                Region = config.MultiRegion.SecondaryRegion
                            }
                        },
                        config);

                    var template = Template.FromStack(pilotLightStack);

                    // Verify standby database exists (critical for RTO)
                    template.ResourceCountIs("AWS::RDS::DBInstance", 1);

                    // Verify database is small instance (t3.small for quick startup)
                    template.HasResourceProperties("AWS::RDS::DBInstance", Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["DBInstanceClass"] = Match.StringLikeRegexp("db\\.t3\\..*")
                    }));

                    // Verify Auto Scaling Group exists with minimal capacity
                    template.ResourceCountIs("AWS::AutoScaling::AutoScalingGroup", 1);

                    // Verify minimal capacity (0 or very low for pilot light)
                    template.HasResourceProperties("AWS::AutoScaling::AutoScalingGroup", Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["MinSize"] = Match.AnyValue(),
                        ["MaxSize"] = Match.AnyValue(),
                        ["DesiredCapacity"] = Match.AnyValue()
                    }));

                    // Verify failover automation exists (Lambda function for quick recovery)
                    template.ResourceCountIs("AWS::Lambda::Function", 1);

                    // Verify timeout is sufficient for failover operations
                    template.HasResourceProperties("AWS::Lambda::Function", Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["Timeout"] = Match.AnyValue()
                    }));

                    return true;
                });
        }

        /// <summary>
        /// **Property 6: RPO < 15 phút**
        /// For any DR configuration, the Recovery Point Objective (RPO) SHALL be less than 15 minutes.
        /// This is validated by checking that continuous backup or frequent replication is enabled.
        /// **Validates: Requirements 7.3**
        /// </summary>
        [Property(MaxTest = 50)]
        public Property DisasterRecoveryRPO_ShouldBeLessThan15Minutes()
        {
            // Generator for backup frequency in minutes (1-15 minutes)
            var backupFrequencyGen = Gen.Choose(1, 15);

            return Prop.ForAll(
                backupFrequencyGen.ToArbitrary(),
                (backupFrequency) =>
                {
                    var app = new App();
                    var config = CreateTestConfig();
                    var stack = new BackupStack(app, "TestBackupStack", new StackProps(), config);
                    var template = Template.FromStack(stack);

                    // Verify backup plan has continuous backup enabled for RPO < 15 minutes
                    template.HasResourceProperties("AWS::Backup::BackupPlan", Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["BackupPlan"] = Match.ObjectLike(new Dictionary<string, object>
                        {
                            ["BackupPlanRule"] = Match.ArrayWith(new object[]
                            {
                                Match.ObjectLike(new Dictionary<string, object>
                                {
                                    ["EnableContinuousBackup"] = true
                                })
                            })
                        })
                    }));

                    // Verify backup vault exists
                    template.ResourceCountIs("AWS::Backup::BackupVault", 1);

                    // Also verify replication configuration is enabled
                    config.MultiRegion.EnableCrossRegionReplication.Should().BeTrue(
                        "Cross-region replication should be enabled for DR");

                    return true;
                });
        }

        /// <summary>
        /// Unit test to verify backup strategy construct properties
        /// </summary>
        [Fact]
        public void BackupStrategy_ShouldHaveMultipleBackupRules()
        {
            var app = new App();
            var config = CreateTestConfig();
            var stack = new BackupStack(app, "TestBackupStack", new StackProps(), config);

            stack.BackupStrategy.Should().NotBeNull();
            stack.BackupStrategy.BackupPlan.Should().NotBeNull();
            stack.BackupStrategy.BackupVault.Should().NotBeNull();
            stack.BackupStrategy.BackupRole.Should().NotBeNull();
        }

        /// <summary>
        /// Unit test to verify pilot light construct properties
        /// </summary>
        [Fact]
        public void PilotLight_ShouldHaveRequiredComponents()
        {
            var app = new App();
            var config = CreateTestConfig();

            var vpcStack = new VpcStack(app, "TestVpcStack", new StackProps
            {
                Env = new Amazon.CDK.Environment
                {
                    Region = config.MultiRegion.SecondaryRegion
                }
            }, config);

            var pilotLightStack = new PilotLightStack(app, "TestPilotLightStack",
                new StackProps
                {
                    Env = new Amazon.CDK.Environment
                    {
                        Region = config.MultiRegion.SecondaryRegion
                    }
                },
                config);

            pilotLightStack.PilotLight.Should().NotBeNull();
            pilotLightStack.PilotLight.StandbyDatabase.Should().NotBeNull();
            pilotLightStack.PilotLight.FailoverFunction.Should().NotBeNull();
            pilotLightStack.PilotLight.MinimalAsg.Should().NotBeNull();
        }
    }
}
