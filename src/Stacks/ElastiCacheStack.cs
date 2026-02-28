using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ElastiCache;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// ElastiCache Redis Cluster Stack with multi-AZ and automatic failover
    /// Implements Requirements 8.4
    /// </summary>
    public class ElastiCacheStack : BaseStack
    {
        public CfnReplicationGroup ReplicationGroup { get; }

        public ElastiCacheStack(
            Construct scope,
            string id,
            IStackProps props,
            StackConfiguration config,
            IVpc vpc,
            ISecurityGroup cacheSecurityGroup)
            : base(scope, id, props, config)
        {
            // Create subnet group for ElastiCache
            var subnetGroup = new CfnSubnetGroup(this, "SubnetGroup", new CfnSubnetGroupProps
            {
                Description = "Subnet group for ElastiCache Redis cluster",
                SubnetIds = vpc.SelectSubnets(new SubnetSelection 
                { 
                    SubnetType = SubnetType.PRIVATE_ISOLATED 
                }).SubnetIds,
                CacheSubnetGroupName = GenerateResourceName("cache-subnet-group")
            });

            // Create ElastiCache Redis replication group with cluster mode
            ReplicationGroup = new CfnReplicationGroup(this, "ReplicationGroup", new CfnReplicationGroupProps
            {
                ReplicationGroupId = GenerateResourceName("redis"),
                ReplicationGroupDescription = "Redis cluster with multi-AZ and automatic failover",
                Engine = "redis",
                EngineVersion = "7.0",
                CacheNodeType = "cache.r6g.large",
                NumNodeGroups = 3,
                ReplicasPerNodeGroup = 2,
                MultiAzEnabled = true,
                AutomaticFailoverEnabled = true,
                AtRestEncryptionEnabled = true,
                TransitEncryptionEnabled = true,
                SecurityGroupIds = new[] { cacheSecurityGroup.SecurityGroupId },
                CacheSubnetGroupName = subnetGroup.CacheSubnetGroupName,
                SnapshotRetentionLimit = 7,
                SnapshotWindow = "03:00-05:00",
                PreferredMaintenanceWindow = "sun:05:00-sun:07:00",
                AutoMinorVersionUpgrade = true
            });

            ReplicationGroup.AddDependency(subnetGroup);

            // Create outputs
            CreateOutput("ReplicationGroupId", ReplicationGroup.ReplicationGroupId ?? "N/A", "Redis Replication Group ID");
            CreateOutput("PrimaryEndpoint", ReplicationGroup.AttrConfigurationEndPointAddress ?? "N/A", "Redis Primary Endpoint");
            CreateOutput("ReaderEndpoint", ReplicationGroup.AttrReaderEndPointAddress ?? "N/A", "Redis Reader Endpoint");
        }
    }

    /// <summary>
    /// Props for ElastiCacheStack - used by tests
    /// </summary>
    public class ElastiCacheStackProps : StackProps
    {
        public string Environment { get; set; } = "test";
        public bool ClusterModeEnabled { get; set; } = true;
        public int NumNodeGroups { get; set; } = 3;
        public int ReplicasPerNodeGroup { get; set; } = 2;
    }
}
