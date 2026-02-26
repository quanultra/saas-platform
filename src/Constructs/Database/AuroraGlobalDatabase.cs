using Amazon.CDK;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.EC2;
using Constructs;
using System.Collections.Generic;
using RdsInstanceType = Amazon.CDK.AWS.RDS.InstanceType;
using Ec2InstanceType = Amazon.CDK.AWS.EC2.InstanceType;

namespace AwsSapC02Practice.Infrastructure.Constructs.Database
{
    /// <summary>
    /// Props for Aurora Global Database construct
    /// </summary>
    public class AuroraGlobalDatabaseProps
    {
        public IVpc? Vpc { get; set; }
        public ISecurityGroup? SecurityGroup { get; set; }
        public string? DatabaseName { get; set; }
        public string MasterUsername { get; set; } = "admin";
        public string? Environment { get; set; }
        public bool IsPrimaryRegion { get; set; }
        public string? GlobalClusterIdentifier { get; set; }
        public int BackupRetentionDays { get; set; } = 7;
        public bool EnableEncryption { get; set; } = true;
    }

    /// <summary>
    /// Aurora Global Database construct with multi-region support
    /// Implements Requirements 3.4, 8.2
    /// </summary>
    public class AuroraGlobalDatabase : Construct
    {
        public DatabaseCluster? PrimaryCluster { get; private set; }
        public DatabaseCluster? SecondaryCluster { get; private set; }
        public string GlobalClusterIdentifier { get; private set; }

        public AuroraGlobalDatabase(Construct scope, string id, AuroraGlobalDatabaseProps props)
            : base(scope, id)
        {
            GlobalClusterIdentifier = props.GlobalClusterIdentifier ??
                $"global-{props.Environment}-{props.DatabaseName}";

            if (props.IsPrimaryRegion)
            {
                CreatePrimaryCluster(props);
            }
            else
            {
                CreateSecondaryCluster(props);
            }

            // Apply tags
            Tags.Of(this).Add("Component", "AuroraGlobalDatabase");
            Tags.Of(this).Add("DatabaseType", props.IsPrimaryRegion ? "Primary" : "Secondary");
        }

        /// <summary>
        /// Create primary Aurora cluster with global database
        /// </summary>
        private void CreatePrimaryCluster(AuroraGlobalDatabaseProps props)
        {
            // Create subnet group
            var subnetGroup = new SubnetGroup(this, "SubnetGroup", new SubnetGroupProps
            {
                Description = $"Subnet group for {props.DatabaseName}",
                Vpc = props.Vpc!,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS
                },
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // Create parameter group for Aurora PostgreSQL
            var parameterGroup = new ParameterGroup(this, "ParameterGroup", new ParameterGroupProps
            {
                Engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps
                {
                    Version = AuroraPostgresEngineVersion.VER_16_1
                }),
                Description = $"Parameter group for {props.DatabaseName}",
                Parameters = new Dictionary<string, string>
                {
                    { "shared_preload_libraries", "pg_stat_statements" },
                    { "log_statement", "all" },
                    { "log_min_duration_statement", "1000" }
                }
            });

            // Create primary Aurora cluster with writer and reader instances
            PrimaryCluster = new DatabaseCluster(this, "PrimaryCluster", new DatabaseClusterProps
            {
                Engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps
                {
                    Version = AuroraPostgresEngineVersion.VER_16_1
                }),
                Credentials = Credentials.FromGeneratedSecret(props.MasterUsername, new CredentialsBaseOptions
                {
                    SecretName = $"{props.Environment}-{props.DatabaseName}-credentials"
                }),
                DefaultDatabaseName = props.DatabaseName,
                Vpc = props.Vpc!,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS
                },
                SecurityGroups = new[] { props.SecurityGroup! },
                SubnetGroup = subnetGroup,
                ParameterGroup = parameterGroup,
                Writer = ClusterInstance.Provisioned("writer", new ProvisionedClusterInstanceProps
                {
                    InstanceType = Ec2InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MEDIUM),
                    EnablePerformanceInsights = true,
                    PerformanceInsightRetention = PerformanceInsightRetention.DEFAULT
                }),
                Readers = new IClusterInstance[]
                {
                    ClusterInstance.Provisioned("reader1", new ProvisionedClusterInstanceProps
                    {
                        InstanceType = Ec2InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MEDIUM),
                        EnablePerformanceInsights = true,
                        PerformanceInsightRetention = PerformanceInsightRetention.DEFAULT
                    })
                },
                Backup = new BackupProps
                {
                    Retention = Duration.Days(props.BackupRetentionDays),
                    PreferredWindow = "03:00-04:00"
                },
                PreferredMaintenanceWindow = "sun:04:00-sun:05:00",
                StorageEncrypted = props.EnableEncryption,
                CloudwatchLogsExports = new[] { "postgresql" },
                RemovalPolicy = RemovalPolicy.DESTROY,
                DeletionProtection = false // Set to true in production
            });

            // Create CloudFormation outputs
            new CfnOutput(this, "PrimaryClusterEndpoint", new CfnOutputProps
            {
                Value = PrimaryCluster.ClusterEndpoint.Hostname,
                Description = "Primary Aurora cluster endpoint",
                ExportName = $"{props.Environment}-PrimaryClusterEndpoint"
            });

            new CfnOutput(this, "PrimaryClusterReadEndpoint", new CfnOutputProps
            {
                Value = PrimaryCluster.ClusterReadEndpoint.Hostname,
                Description = "Primary Aurora cluster read endpoint",
                ExportName = $"{props.Environment}-PrimaryClusterReadEndpoint"
            });

            new CfnOutput(this, "PrimaryClusterSecretArn", new CfnOutputProps
            {
                Value = PrimaryCluster.Secret!.SecretArn,
                Description = "ARN of the secret containing database credentials",
                ExportName = $"{props.Environment}-PrimaryClusterSecretArn"
            });
        }

        /// <summary>
        /// Create secondary Aurora cluster for global database
        /// </summary>
        private void CreateSecondaryCluster(AuroraGlobalDatabaseProps props)
        {
            // Create subnet group for secondary region
            var subnetGroup = new SubnetGroup(this, "SecondarySubnetGroup", new SubnetGroupProps
            {
                Description = $"Subnet group for secondary {props.DatabaseName}",
                Vpc = props.Vpc!,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS
                },
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // Create secondary Aurora cluster (read replica)
            SecondaryCluster = new DatabaseCluster(this, "SecondaryCluster", new DatabaseClusterProps
            {
                Engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps
                {
                    Version = AuroraPostgresEngineVersion.VER_16_1
                }),
                Vpc = props.Vpc!,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS
                },
                SecurityGroups = new[] { props.SecurityGroup! },
                SubnetGroup = subnetGroup,
                Writer = ClusterInstance.Provisioned("reader", new ProvisionedClusterInstanceProps
                {
                    InstanceType = Ec2InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MEDIUM),
                    EnablePerformanceInsights = true,
                    PerformanceInsightRetention = PerformanceInsightRetention.DEFAULT
                }),
                StorageEncrypted = props.EnableEncryption,
                CloudwatchLogsExports = new[] { "postgresql" },
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // Create CloudFormation outputs
            new CfnOutput(this, "SecondaryClusterEndpoint", new CfnOutputProps
            {
                Value = SecondaryCluster.ClusterEndpoint.Hostname,
                Description = "Secondary Aurora cluster endpoint",
                ExportName = $"{props.Environment}-SecondaryClusterEndpoint"
            });

            new CfnOutput(this, "SecondaryClusterReadEndpoint", new CfnOutputProps
            {
                Value = SecondaryCluster.ClusterReadEndpoint.Hostname,
                Description = "Secondary Aurora cluster read endpoint",
                ExportName = $"{props.Environment}-SecondaryClusterReadEndpoint"
            });
        }
    }
}
