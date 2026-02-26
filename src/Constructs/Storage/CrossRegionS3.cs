using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.IAM;
using Constructs;

namespace AwsSapC02Practice.Infrastructure.Constructs.Storage
{
    /// <summary>
    /// Properties for CrossRegionS3 construct
    /// </summary>
    public class CrossRegionS3Props
    {
        public string PrimaryRegion { get; set; }
        public string SecondaryRegion { get; set; }
        public string BucketPrefix { get; set; }
    }

    /// <summary>
    /// S3 Cross-Region Replication construct
    /// Implements Requirements 3.3, 9.3
    /// </summary>
    public class CrossRegionS3 : Construct
    {
        public IBucket PrimaryBucket { get; }
        public IBucket SecondaryBucket { get; }
        public IRole ReplicationRole { get; }

        public CrossRegionS3(Construct scope, string id, CrossRegionS3Props props)
            : base(scope, id)
        {
            // Create IAM Role for replication
            ReplicationRole = new Role(this, "ReplicationRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("s3.amazonaws.com"),
                Description = "Role for S3 cross-region replication"
            });

            // Create Primary Bucket with versioning and encryption
            PrimaryBucket = new Bucket(this, "PrimaryBucket", new BucketProps
            {
                BucketName = $"{props.BucketPrefix}-primary-{props.PrimaryRegion}",
                Versioned = true, // Required for replication
                Encryption = BucketEncryption.S3_MANAGED,
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                RemovalPolicy = RemovalPolicy.DESTROY,
                AutoDeleteObjects = true,

                LifecycleRules = new[]
                {
                    new LifecycleRule
                    {
                        Id = "TransitionToIA",
                        Enabled = true,
                        Transitions = new[]
                        {
                            new Transition
                            {
                                StorageClass = StorageClass.INFREQUENT_ACCESS,
                                TransitionAfter = Duration.Days(30)
                            },
                            new Transition
                            {
                                StorageClass = StorageClass.GLACIER,
                                TransitionAfter = Duration.Days(90)
                            }
                        }
                    },
                    new LifecycleRule
                    {
                        Id = "ExpireOldVersions",
                        Enabled = true,
                        NoncurrentVersionExpiration = Duration.Days(90)
                    }
                }
            });

            // Create Secondary Bucket (destination)
            SecondaryBucket = new Bucket(this, "SecondaryBucket", new BucketProps
            {
                BucketName = $"{props.BucketPrefix}-secondary-{props.SecondaryRegion}",
                Versioned = true,
                Encryption = BucketEncryption.S3_MANAGED,
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                RemovalPolicy = RemovalPolicy.DESTROY,
                AutoDeleteObjects = true
            });

            // Grant permissions for replication role
            PrimaryBucket.GrantRead(ReplicationRole);
            SecondaryBucket.GrantWrite(ReplicationRole);

            // Add replication configuration to primary bucket
            var cfnBucket = PrimaryBucket.Node.DefaultChild as CfnBucket;
            cfnBucket.ReplicationConfiguration = new CfnBucket.ReplicationConfigurationProperty
            {
                Role = ReplicationRole.RoleArn,
                Rules = new[]
                {
                    new CfnBucket.ReplicationRuleProperty
                    {
                        Status = "Enabled",
                        Priority = 1,
                        Filter = new CfnBucket.ReplicationRuleFilterProperty
                        {
                            Prefix = ""
                        },
                        Destination = new CfnBucket.ReplicationDestinationProperty
                        {
                            Bucket = SecondaryBucket.BucketArn,
                            ReplicationTime = new CfnBucket.ReplicationTimeProperty
                            {
                                Status = "Enabled",
                                Time = new CfnBucket.ReplicationTimeValueProperty
                                {
                                    Minutes = 15
                                }
                            },
                            Metrics = new CfnBucket.MetricsProperty
                            {
                                Status = "Enabled",
                                EventThreshold = new CfnBucket.ReplicationTimeValueProperty
                                {
                                    Minutes = 15
                                }
                            }
                        },
                        DeleteMarkerReplication = new CfnBucket.DeleteMarkerReplicationProperty
                        {
                            Status = "Enabled"
                        }
                    }
                }
            };

            // Add tags
            Tags.Of(this).Add("Component", "CrossRegionReplication");
            Tags.Of(PrimaryBucket).Add("BucketType", "Primary");
            Tags.Of(SecondaryBucket).Add("BucketType", "Secondary");
        }
    }
}
