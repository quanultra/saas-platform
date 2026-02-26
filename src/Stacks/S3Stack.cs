using Amazon.CDK;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Constructs.Storage;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// S3 Stack with Cross-Region Replication
    /// Implements Requirements 3.3, 9.3
    /// </summary>
    public class S3Stack : BaseStack
    {
        public CrossRegionS3 CrossRegionStorage { get; }

        public S3Stack(Construct scope, string id, IStackProps props, StackConfiguration config)
            : base(scope, id, props, config)
        {
            // Create cross-region S3 buckets with replication
            CrossRegionStorage = new CrossRegionS3(this, "CrossRegionStorage", new CrossRegionS3Props
            {
                PrimaryRegion = config.MultiRegion.PrimaryRegion,
                SecondaryRegion = config.MultiRegion.SecondaryRegion,
                BucketPrefix = GenerateResourceName("data")
            });

            // Create CloudFormation outputs
            CreateOutput(
                "PrimaryBucketName",
                CrossRegionStorage.PrimaryBucket.BucketName,
                $"Primary S3 bucket in {config.MultiRegion.PrimaryRegion}"
            );

            CreateOutput(
                "PrimaryBucketArn",
                CrossRegionStorage.PrimaryBucket.BucketArn,
                "Primary S3 bucket ARN"
            );

            CreateOutput(
                "SecondaryBucketName",
                CrossRegionStorage.SecondaryBucket.BucketName,
                $"Secondary S3 bucket in {config.MultiRegion.SecondaryRegion}"
            );

            CreateOutput(
                "SecondaryBucketArn",
                CrossRegionStorage.SecondaryBucket.BucketArn,
                "Secondary S3 bucket ARN"
            );

            CreateOutput(
                "ReplicationRoleArn",
                CrossRegionStorage.ReplicationRole.RoleArn,
                "IAM role ARN for S3 replication"
            );
        }
    }
}
