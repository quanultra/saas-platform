using Amazon.CDK;
using Amazon.CDK.AWS.CloudTrail;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.KMS;
using Amazon.CDK.AWS.Logs;
using Constructs;

namespace AwsSapC02Practice.Infrastructure.Stacks;

public class CloudTrailStack : Stack
{
    public Trail Trail { get; }
    public IBucket LogBucket { get; }

    public CloudTrailStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        var trailKey = new Key(this, "CloudTrailKey", new KeyProps
        {
            Description = "KMS key for CloudTrail logs encryption",
            EnableKeyRotation = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            Alias = "alias/cloudtrail-key"
        });

        LogBucket = new Bucket(this, "CloudTrailLogBucket", new BucketProps
        {
            Encryption = BucketEncryption.KMS,
            EncryptionKey = trailKey,
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Versioned = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            LifecycleRules = new[]
            {
                new LifecycleRule
                {
                    Enabled = true,
                    Transitions = new[]
                    {
                        new Transition
                        {
                            StorageClass = StorageClass.GLACIER,
                            TransitionAfter = Duration.Days(90)
                        }
                    },
                    Expiration = Duration.Days(365)
                }
            }
        });

        var logGroup = new LogGroup(this, "CloudTrailLogGroup", new LogGroupProps
        {
            LogGroupName = "/aws/cloudtrail/multi-region-trail",
            Retention = RetentionDays.ONE_MONTH,
            RemovalPolicy = RemovalPolicy.DESTROY,
            EncryptionKey = trailKey
        });

        Trail = new Trail(this, "MultiRegionTrail", new TrailProps
        {
            TrailName = "multi-region-trail",
            Bucket = LogBucket,
            EncryptionKey = trailKey,
            CloudWatchLogGroup = logGroup,
            IsMultiRegionTrail = true,
            IncludeGlobalServiceEvents = true,
            EnableFileValidation = true,
            ManagementEvents = ReadWriteType.ALL,
            SendToCloudWatchLogs = true,
            InsightTypes = new[]
            {
                InsightType.API_CALL_RATE,
                InsightType.API_ERROR_RATE
            }
        });

        new CfnOutput(this, "TrailArn", new CfnOutputProps
        {
            Value = Trail.TrailArn,
            ExportName = "CloudTrailArn"
        });

        new CfnOutput(this, "LogBucketName", new CfnOutputProps
        {
            Value = LogBucket.BucketName,
            ExportName = "CloudTrailLogBucketName"
        });

        Amazon.CDK.Tags.Of(this).Add("Component", "Security");
        Amazon.CDK.Tags.Of(this).Add("Service", "CloudTrail");
    }
}
