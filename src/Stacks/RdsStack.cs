using Amazon.CDK;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Constructs.Database;
using Amazon.CDK.AWS.EC2;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// RDS Stack with Aurora Global Database
    /// Implements Requirements 3.4, 8.2
    /// </summary>
    public class RdsStack : BaseStack
    {
        public AuroraGlobalDatabase GlobalDatabase { get; }

        public RdsStack(
            Construct scope,
            string id,
            IStackProps props,
            StackConfiguration config,
            IVpc vpc,
            ISecurityGroup dbSecurityGroup,
            bool isPrimaryRegion = true)
            : base(scope, id, props, config)
        {
            // Create Aurora Global Database
            GlobalDatabase = new AuroraGlobalDatabase(this, "GlobalDatabase", new AuroraGlobalDatabaseProps
            {
                Vpc = vpc,
                SecurityGroup = dbSecurityGroup,
                DatabaseName = config.Database.DatabaseName ?? "sapc02db",
                MasterUsername = "admin",
                Environment = config.Environment,
                IsPrimaryRegion = isPrimaryRegion,
                GlobalClusterIdentifier = $"{config.ProjectName}-{config.Environment}-global-cluster",
                BackupRetentionDays = config.Database.BackupRetentionDays,
                EnableEncryption = config.Database.EnableEncryption
            });

            // Create stack-level outputs
            if (isPrimaryRegion)
            {
                CreateOutput(
                    "GlobalClusterIdentifier",
                    GlobalDatabase.GlobalClusterIdentifier,
                    "Aurora Global Database cluster identifier"
                );

                CreateOutput(
                    "PrimaryRegion",
                    config.MultiRegion.PrimaryRegion,
                    "Primary region for Aurora Global Database"
                );
            }
            else
            {
                CreateOutput(
                    "SecondaryRegion",
                    config.MultiRegion.SecondaryRegion,
                    "Secondary region for Aurora Global Database"
                );
            }
        }
    }
}
