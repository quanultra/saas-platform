using Amazon.CDK;
using Amazon.CDK.AWS.KMS;
using Amazon.CDK.AWS.IAM;
using Constructs;

namespace AwsSapC02Practice.Stacks;

public class KmsStack : Stack
{
    public IKey S3EncryptionKey { get; }
    public IKey RdsEncryptionKey { get; }
    public IKey EbsEncryptionKey { get; }

    public KmsStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        S3EncryptionKey = new Key(this, "S3EncryptionKey", new KeyProps
        {
            Description = "KMS key for S3 bucket encryption",
            EnableKeyRotation = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            Alias = "alias/s3-encryption-key"
        });

        RdsEncryptionKey = new Key(this, "RdsEncryptionKey", new KeyProps
        {
            Description = "KMS key for RDS database encryption",
            EnableKeyRotation = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            Alias = "alias/rds-encryption-key"
        });

        EbsEncryptionKey = new Key(this, "EbsEncryptionKey", new KeyProps
        {
            Description = "KMS key for EBS volume encryption",
            EnableKeyRotation = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            Alias = "alias/ebs-encryption-key"
        });

        new CfnOutput(this, "S3KeyArn", new CfnOutputProps
        {
            Value = S3EncryptionKey.KeyArn,
            ExportName = "KmsS3KeyArn"
        });

        new CfnOutput(this, "RdsKeyArn", new CfnOutputProps
        {
            Value = RdsEncryptionKey.KeyArn,
            ExportName = "KmsRdsKeyArn"
        });

        new CfnOutput(this, "EbsKeyArn", new CfnOutputProps
        {
            Value = EbsEncryptionKey.KeyArn,
            ExportName = "KmsEbsKeyArn"
        });

        Amazon.CDK.Tags.Of(this).Add("Component", "Security");
        Amazon.CDK.Tags.Of(this).Add("Service", "KMS");
    }
}
