using Amazon.CDK;
using Amazon.CDK.AWS.Backup;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Constructs.DisasterRecovery;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// AWS Backup Stack for Disaster Recovery
    /// Implements Requirements 7.1, 8.4
    /// </summary>
    public class BackupStack : BaseStack
    {
        public BackupStrategy BackupStrategy { get; }

        public BackupStack(Construct scope, string id, IStackProps props, StackConfiguration config)
            : base(scope, id, props, config)
        {
            // Generate backup vault name
            var backupVaultName = GenerateResourceName("backup-vault");

            // Create backup strategy with cross-region support
            BackupStrategy = new BackupStrategy(this, "BackupStrategy", new BackupStrategyProps
            {
                BackupVaultName = backupVaultName,
                EnableCrossRegionBackup = config.MultiRegion.EnableCrossRegionReplication,
                DestinationRegion = config.MultiRegion.SecondaryRegion,
                ResourceArns = new List<string>() // Resources will be added via tags or explicit ARNs
            });

            // Create CloudFormation outputs
            CreateOutput(
                "BackupVaultName",
                BackupStrategy.BackupVault.BackupVaultName,
                "Name of the backup vault"
            );

            CreateOutput(
                "BackupVaultArn",
                BackupStrategy.BackupVault.BackupVaultArn,
                "ARN of the backup vault"
            );

            CreateOutput(
                "BackupPlanId",
                BackupStrategy.BackupPlan.BackupPlanId,
                "ID of the backup plan"
            );

            CreateOutput(
                "BackupPlanArn",
                BackupStrategy.BackupPlan.BackupPlanArn,
                "ARN of the backup plan"
            );

            CreateOutput(
                "BackupRoleArn",
                BackupStrategy.BackupRole.RoleArn,
                "ARN of the IAM role for backup operations"
            );
        }

        /// <summary>
        /// Add resources to backup by ARN
        /// </summary>
        public void AddResourceToBackup(string resourceArn)
        {
            BackupStrategy.BackupPlan.AddSelection($"Selection-{resourceArn.GetHashCode()}", new BackupSelectionOptions
            {
                Resources = new BackupResource[] { BackupResource.FromArn(resourceArn) },
                Role = BackupStrategy.BackupRole,
                AllowRestores = true
            });
        }

        /// <summary>
        /// Add resources to backup by tag
        /// </summary>
        public void AddResourcesByTag(string tagKey, string tagValue)
        {
            BackupStrategy.BackupPlan.AddSelection($"Selection-Tag-{tagKey}", new BackupSelectionOptions
            {
                Resources = new BackupResource[]
                {
                    BackupResource.FromTag(tagKey, tagValue)
                },
                Role = BackupStrategy.BackupRole,
                AllowRestores = true
            });
        }
    }
}
