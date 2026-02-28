# BackupStack - AWS Backup Implementation

## Overview

The `BackupStack` implements AWS Backup for disaster recovery, providing automated backup plans with multiple retention policies and cross-region backup capabilities.

## Features

- **Backup Vault**: Encrypted backup storage
- **Multiple Backup Plans**:
  - Daily backups with 35-day retention (30 days in warm storage, then cold storage)
  - Weekly backups with 90-day retention (60 days in warm storage, then cold storage)
  - Monthly backups with 365-day retention
- **Continuous Backup**: Enabled for daily backups (point-in-time recovery)
- **Cross-Region Support**: Configuration
"dev",
    ProjectName = "aws-sap-c02-practice",
    MultiRegion = new MultiRegionConfig
    {
        PrimaryRegion = "us-east-1",
        SecondaryRegion = "eu-west-1",
        EnableCrossRegionReplication = true
    }
};

var backupStack = new BackupStack(app, "BackupStack", new StackProps
{
    Env = new Environment
    {
        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
        Region = "us-east-1"
    }
}, config);

app.Synth();
```

### Adding Resources to Backup

#### By ARN

```csharp
// Add specific resources by ARN
backupStack.AddResourceToBackup("arn:aws:rds:us-east-1:123456789012:db:my-database");
backupStack.AddResourceToBackup("arn:aws:dynamodb:us-east-1:123456789012:table/my-table");
```

#### By Tag

```csharp
// Add all resources with specific tag
backupStack.AddResourcesByTag("Backup", "true");
backupStack.AddResourcesByTag("Environment", "production");
```

## Backup Schedule

| Backup Type | Schedule | Retention | Cold Storage After |
|------------|----------|-----------|-------------------|
| Daily | 2:00 AM UTC | 35 days | 30 days |
| Weekly | 3:00 AM UTC (Sunday) | 90 days | 60 days |
| Monthly | 4:00 AM UTC (1st of month) | 365 days | N/A |

## Outputs

The stack creates the following CloudFormation outputs:

- `BackupVaultName`: Name of the backup vault
- `BackupVaultArn`: ARN of the backup vault
- `BackupPlanId`: ID of the backup plan
- `BackupPlanArn`: ARN of the backup plan
- `BackupRoleArn`: ARN of the IAM role for backup operations

## Cross-Region Backup

Cross-region backup copy is configured when `EnableCrossRegionReplication` is set to `true` in the configuration. The implementation uses the destination region specified in `MultiRegion.SecondaryRegion`.

**Note**: Full cross-region backup copy implementation requires additional L1 construct configuration. The current implementation provides the foundation and documents the approach for production use.

## Cost Considerations

- Backup storage costs vary by region and storage tier (warm vs. cold)
- Cross-region data transfer incurs additional charges
- Continuous backup (point-in-time recovery) has additional costs
- Use lifecycle policies to move backups to cold storage for cost optimization

## Best Practices

1. **Tag Resources**: Use consistent tagging for resources you want to backup
2. **Test Restores**: Regularly test backup restoration procedures
3. **Monitor Backup Jobs**: Set up CloudWatch alarms for failed backup jobs
4. **Review Retention**: Adjust retention policies based on compliance requirements
5. **Cross-Region**: Enable cross-region backup for critical workloads

## Integration with Other Stacks

The BackupStack can be integrated with other stacks to automatically backup their resources:

```csharp
// Create RDS stack
var rdsStack = new RdsStack(app, "RdsStack", stackProps, config);

// Create backup stack
var backupStack = new BackupStack(app, "BackupStack", stackProps, config);

// Add RDS resources to backup by tag
backupStack.AddResourcesByTag("Component", "Database");
```

## Supported AWS Services

AWS Backup supports the following services:
- Amazon RDS
- Amazon DynamoDB
- Amazon EFS
- Amazon EBS
- Amazon EC2
- Amazon Aurora
- AWS Storage Gateway
- And more...

## References

- [AWS Backup Documentation](https://docs.aws.amazon.com/aws-backup/)
- [AWS CDK Backup Module](https://docs.aws.amazon.com/cdk/api/v2/docs/aws-cdk-lib.aws_backup-readme.html)
- [AWS Backup Pricing](https://aws.amazon.com/backup/pricing/)
