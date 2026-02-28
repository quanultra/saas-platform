using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// Aurora Multi-AZ Cluster Stack with read replicas and automated failover
    /// Implements Requirements 8.3, 8.9
    /// </summary>
    public class AuroraStack : BaseStack
    {
        public IDatabaseCluster DatabaseCluster { get; }

        public AuroraStack(
            Construct scope,
            string id,
            IStackProps props,
            StackConfiguration config,
            IVpc vpc,
            ISecurityGroup dbSecurityGroup)
            : base(scope, id, props, config)
        {
            // Create Aurora PostgreSQL cluster with multi-AZ
            DatabaseCluster = new DatabaseCluster(this, "AuroraCluster", new DatabaseClusterProps
            {
                Engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps
                {
                    Version = AuroraPostgresEngineVersion.VER_16_4
                }),
                Credentials = Credentials.FromGeneratedSecret("admin"),
                DefaultDatabaseName = config.Database.DatabaseName ?? "sapc02db",
                Writer = ClusterInstance.Provisioned("writer", new ProvisionedClusterInstanceProps
                {
                    InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MEDIUM)
                }),
                Readers = new IClusterInstance[]
                {
                    ClusterInstance.Provisioned("reader1", new ProvisionedClusterInstanceProps
                    {
                        InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MEDIUM)
                    })
                },
                Vpc = vpc,
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
                SecurityGroups = new[] { dbSecurityGroup },
                StorageEncrypted = true,
                Backup = new BackupProps
                {
                    Retention = Duration.Days(config.Database.BackupRetentionDays),
                    PreferredWindow = "03:00-04:00"
                },
                PreferredMaintenanceWindow = "sun:04:00-sun:05:00",
                CloudwatchLogsExports = new[] { "postgresql" },
                CloudwatchLogsRetention = Amazon.CDK.AWS.Logs.RetentionDays.ONE_MONTH,
                DeletionProtection = config.Environment == "prod",
                RemovalPolicy = config.Environment == "prod" ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
            });

            // Create outputs
            CreateOutput("ClusterEndpoint", DatabaseCluster.ClusterEndpoint.Hostname, "Aurora Cluster Endpoint");
            CreateOutput("ClusterReadEndpoint", DatabaseCluster.ClusterReadEndpoint.Hostname, "Aurora Read Endpoint");
            CreateOutput("ClusterIdentifier", DatabaseCluster.ClusterIdentifier, "Aurora Cluster Identifier");
        }
    }

    /// <summary>
    /// Props for AuroraStack - used by tests
    /// </summary>
    public class AuroraStackProps : StackProps
    {
        public string Environment { get; set; } = "test";
        public string DatabaseName { get; set; } = "testdb";
        public int ReadReplicaCount { get; set; } = 1;
    }
}
