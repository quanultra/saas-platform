using Amazon.CDK;
using Amazon.CDK.AWS.Backup;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.IAM;
using Constructs;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Constructs.DisasterRecovery
{
    /// <summary>
    /// Properties for BackupStrategy construct
    /// </summary>
    public class BackupStrategyProps
    {
        public string BackupVaultName { get; set; }
        public List<string> ResourceArns { get; set; }
        public bool EnableCrossRegionBackup { get; set; }
        public string DestinationRegion { get; set; }
        public string DestinationVaultArn { get; set; }

        public BackupStrategyProps()
        {
            ResourceArns = new List<string>();
            EnableCrossRegionBackup = true;
        }
    }

    /// <summary>
    /// AWS Backup Strategy construct
    /// Implements Requirements 7.1, 8.4
    /// </summary>
    public class BackupStrategy : Construct
    {
        public BackupVault BackupVault { get; }
        public BackupPlan BackupPlan { get; }
        public IRole BackupRole { get; }

        public BackupStrategy(Construct scope, string id, BackupStrategyProps props)
            : base(scope, id)
        {
            // Create Backup Vault with encryption
            BackupVault = new BackupVault(this, "BackupVault", new BackupVaultProps
            {
                BackupVaultName = props.BackupVaultName,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // Create IAM role for backup operations
            BackupRole = new Role(this, "BackupRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("backup.amazonaws.com"),
                Description = "IAM role for AWS Backup service",
                ManagedPolicies = new IManagedPolicy[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSBackupServiceRolePolicyForBackup"),
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSBackupServiceRolePolicyForRestores")
                }
            });

            // Create backup plan rules
            var backupRules = new List<BackupPlanRule>();

            // Daily backup with continuous backup enabled
            // Daily backup with continuous backup enabled
            var dailyRule = new BackupPlanRule(new BackupPlanRuleProps
            {
                RuleName = "DailyBackup",
                BackupVault = BackupVault,
                ScheduleExpression = Schedule.Cron(new CronOptions
                {
                    Hour = "2",
                    Minute = "0"
                }),
                DeleteAfter = Duration.Days(35),
                // Note: MoveToColdStorageAfter cannot be used with EnableContinuousBackup
                EnableContinuousBackup = true
            });
            backupRules.Add(dailyRule);
            // Weekly backup for longer retention
            var weeklyRule = new BackupPlanRule(new BackupPlanRuleProps
            {
                RuleName = "WeeklyBackup",
                BackupVault = BackupVault,
                ScheduleExpression = Schedule.Cron(new CronOptions
                {
                    Hour = "3",
                    Minute = "0",
                    WeekDay = "SUN"
                }),
                DeleteAfter = Duration.Days(90),
                MoveToColdStorageAfter = Duration.Days(60)
            });
            backupRules.Add(weeklyRule);

            // Monthly backup for compliance
            var monthlyRule = new BackupPlanRule(new BackupPlanRuleProps
            {
                RuleName = "MonthlyBackup",
                BackupVault = BackupVault,
                ScheduleExpression = Schedule.Cron(new CronOptions
                {
                    Hour = "4",
                    Minute = "0",
                    Day = "1"
                }),
                DeleteAfter = Duration.Days(365)
            });
            backupRules.Add(monthlyRule);

            // Create the backup plan
            BackupPlan = new BackupPlan(this, "BackupPlan", new BackupPlanProps
            {
                BackupPlanName = $"{props.BackupVaultName}-plan",
                BackupPlanRules = backupRules.ToArray()
            });

            // Configure cross-region backup copy if enabled
            if (props.EnableCrossRegionBackup && !string.IsNullOrEmpty(props.DestinationRegion))
            {
                ConfigureCrossRegionBackup(props.DestinationRegion, props.DestinationVaultArn);
            }

            // Add backup selections for resources
            if (props.ResourceArns != null && props.ResourceArns.Count > 0)
            {
                var resources = new List<BackupResource>();
                foreach (var arn in props.ResourceArns)
                {
                    resources.Add(BackupResource.FromArn(arn));
                }

                BackupPlan.AddSelection("BackupSelection", new BackupSelectionOptions
                {
                    Resources = resources.ToArray(),
                    Role = BackupRole,
                    AllowRestores = true
                });
            }

            // Add tags
            Tags.Of(this).Add("Component", "DisasterRecovery");
            Tags.Of(this).Add("BackupType", "Automated");
        }

        /// <summary>
        /// Configure cross-region backup copy rules
        /// Note: Cross-region copy requires additional setup using L1 constructs
        /// </summary>
        private void ConfigureCrossRegionBackup(string destinationRegion, string destinationVaultArn)
        {
            // Cross-region backup copy configuration
            // The AWS CDK high-level BackupPlanRule API doesn't directly support CopyActions
            // To implement cross-region backup copy in production, you would need to:
            // 1. Create a backup vault in the destination region
            // 2. Use CfnBackupPlan (L1 construct) to configure CopyActions
            // 3. Or use CloudFormation escape hatches to modify the generated template

            // This is documented here for future implementation
            // Example implementation would use CfnBackupPlan.BackupPlanResourceTypeProperty
            // with CopyActions configured to copy backups to the destination region
        }
    }
}
