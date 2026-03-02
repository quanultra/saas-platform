# AWS CDK Stack Lifecycle Management Scripts

This directory contains scripts for managing the complete lifecycle of AWS CDK stacks including deployment, updates, and deletion.

## Overview

The lifecycle management scripts provide:

- **Deployment**: Deploy stacks with error handling, dependency management, and rollback support
- **Updates**: Update stacks with change set validation and blue-green deployment options
- **Deletion**: Delete stacks with resource retention and cleanup capabilities
- **Rollback**: Rollback failed deployments to previous stable state

## Prerequisites

### R
ploy-Stack.ps1

Deploy CDK stacks with comprehensive error handling and validation.

#### Usage

**Bash:**
```bash
./scripts/deploy-stack.sh -e <environment> [OPTIONS]
```

**PowerShell:**
```powershell
.\scripts\Deploy-Stack.ps1 -Environment <environment> [OPTIONS]
```

#### Options

- `-e, --environment ENV` (required): Environment to deploy (dev, staging, prod)
- `-s, --stack STACK`: Specific stack to deploy (deploys all if not specified)
- `-r, --region REGION`: AWS region (default: us-east-1)
- `-a, --account ACCOUNT`: AWS account ID (auto-detected if not provided)
- `-d, --dry-run`: Perform dry run (synth only, no deployment)
- `--skip-tests`: Skip running tests before deployment
- `--auto-approve`: Skip approval prompts
- `--rollback-on-failure`: Automatically rollback on deployment failure

#### Examples

```bash
# Deploy all stacks to dev environment
./scripts/deploy-stack.sh -e dev

# Deploy specific stack to production
./scripts/deploy-stack.sh -e prod -s VpcStack -a 123456789012

# Dry run to see what would be deployed
./scripts/deploy-stack.sh -e staging --dry-run

# Deploy with automatic rollback on failure
./scripts/deploy-stack.sh -e dev --rollback-on-failure --auto-approve
```

#### Features

- **Prerequisite Checking**: Validates all required tools are installed
- **Build & Test**: Builds project and runs tests before deployment
- **CDK Bootstrap**: Automatically bootstraps CDK if needed
- **Dependency Management**: Deploys stacks in correct order based on dependencies
- **Error Handling**: Comprehensive error handling with detailed logging
- **Rollback Support**: Optional automatic rollback on failure
- **Verification**: Validates deployment success after completion

### 2. update-stack.sh

Update existing stacks with change set validation and deployment strategies.

#### Usage

```bash
./scripts/update-stack.sh -e <environment> -s <stack> [OPTIONS]
```

#### Options

- `-e, --environment ENV` (required): Environment (dev, staging, prod)
- `-s, --stack STACK` (required): Stack to update
- `-r, --region REGION`: AWS region (default: us-east-1)
- `-a, --account ACCOUNT`: AWS account ID
- `--change-set-only`: Create change set without executing
- `--review-changes`: Review changes before applying
- `--blue-green`: Use blue-green deployment strategy
- `--canary`: Use canary deployment (gradual rollout)
- `--auto-approve`: Skip approval prompts

#### Examples

```bash
# Update stack with change review
./scripts/update-stack.sh -e dev -s VpcStack --review-changes

# Create change set only (don't execute)
./scripts/update-stack.sh -e prod -s EcsStack --change-set-only

# Blue-green deployment for zero downtime
./scripts/update-stack.sh -e prod -s ApiGatewayStack --blue-green

# Canary deployment with gradual rollout
./scripts/update-stack.sh -e prod -s ServerlessStack --canary
```

#### Features

- **Change Set Creation**: Creates CloudFormation change sets for review
- **Risk Analysis**: Analyzes changes and categorizes by risk level
- **Change Review**: Interactive review of changes before execution
- **Blue-Green Deployment**: Zero-downtime deployment strategy
- **Canary Deployment**: Gradual rollout with monitoring
- **Drift Detection**: Checks for configuration drift after update
- **Validation**: Validates stack health after update

### 3. delete-stack.sh

Delete stacks with resource retention and cleanup options.

#### Usage

```bash
./scripts/delete-stack.sh -e <environment> [OPTIONS]
```

#### Options

- `-e, --environment ENV` (required): Environment (dev, staging, prod)
- `-s, --stack STACK`: Specific stack to delete (deletes all if not specified)
- `-r, --region REGION`: AWS region (default: us-east-1)
- `--retain-resources`: Retain resources instead of deleting
- `--retain-data`: Retain data resources (S3, RDS, DynamoDB)
- `--force`: Force deletion without confirmation
- `--cleanup-retained`: Cleanup previously retained resources
- `--dry-run`: Show what would be deleted without actually deleting

#### Examples

```bash
# Delete specific stack with confirmation
./scripts/delete-stack.sh -e dev -s VpcStack

# Delete all stacks in environment
./scripts/delete-stack.sh -e dev

# Retain data resources during deletion
./scripts/delete-stack.sh -e staging --retain-data

# Dry run to see what would be deleted
./scripts/delete-stack.sh -e dev --dry-run

# Cleanup previously retained resources
./scripts/delete-stack.sh -e dev --cleanup-retained
```

#### Features

- **Resource Identification**: Identifies data resources before deletion
- **S3 Bucket Emptying**: Automatically empties S3 buckets before deletion
- **RDS Snapshots**: Creates final snapshots for databases
- **Resource Retention**: Option to retain specific resources
- **Deletion Order**: Deletes stacks in correct order (reverse of deployment)
- **Cleanup**: Cleanup previously retained resources
- **Safety Checks**: Confirmation prompts and dry-run mode

### 4. rollback-stack.sh

Rollback failed stack deployments.

#### Usage

```bash
./scripts/rollback-stack.sh -e <environment> -s <stack> [OPTIONS]
```

#### Options

- `-e, --environment ENV` (required): Environment (dev, staging, prod)
- `-s, --stack STACK` (required): Stack name to rollback
- `-r, --region REGION`: AWS region (default: us-east-1)
- `--to-version VERSION`: Rollback to specific version
- `--auto-approve`: Skip confirmation prompts

#### Examples

```bash
# Rollback failed stack update
./scripts/rollback-stack.sh -e dev -s VpcStack

# Rollback to specific version
./scripts/rollback-stack.sh -e prod -s EcsStack --to-version 5

# Automatic rollback without confirmation
./scripts/rollback-stack.sh -e staging -s ApiGatewayStack --auto-approve
```

#### Features

- **Status Detection**: Detects current stack status
- **Event History**: Shows recent stack events
- **Automatic Rollback**: Continues rollback for failed updates
- **Version Rollback**: Rollback to specific previous version (if available)

## C# Classes

The `src/Models/` directory contains C# classes for programmatic lifecycle management:

### StackDependencyManager

Manages stack dependencies and determines deployment order.

```csharp
var manager = StackDependencyManager.ConfigureStandardDependencies();
var deploymentOrder = manager.GetDeploymentOrder();
```

### StackUpdateManager

Manages stack updates with change set validation.

```csharp
var updateManager = new StackUpdateManager(cloudFormationClient, region);
var changeSet = await updateManager.CreateChangeSetAsync(stackName, templateBody);
var risk = updateManager.AnalyzeChangeRisk(changeSet);
await updateManager.ExecuteChangeSetAsync(stackName, changeSet.ChangeSetName);
```

### StackDeletionManager

Manages stack deletion with resource retention.

```csharp
var deletionManager = new StackDeletionManager(cfClient, s3Client, rdsClient, region);
var options = new DeletionOptions
{
    RetainData = true,
    CreateFinalSnapshot = true
};
var result = await deletionManager.DeleteStackAsync(stackName, options);
```

## Logging

All scripts create detailed logs in the `logs/` directory:

- `deployment-YYYYMMDD-HHMMSS.log`: Deployment logs
- `update-YYYYMMDD-HHMMSS.log`: Update logs
- `deletion-YYYYMMDD-HHMMSS.log`: Deletion logs
- `rollback-YYYYMMDD-HHMMSS.log`: Rollback logs

## Best Practices

### Deployment

1. Always run tests before deploying to production
2. Use `--dry-run` to preview changes
3. Enable `--rollback-on-failure` for production deployments
4. Review logs after deployment

### Updates

1. Create change sets first with `--change-set-only`
2. Review changes carefully before executing
3. Use `--blue-green` for critical production updates
4. Monitor CloudWatch alarms during canary deployments

### Deletion

1. Always use `--dry-run` first to see what will be deleted
2. Use `--retain-data` for production environments
3. Create final snapshots for databases
4. Verify retained resources after deletion

## Troubleshooting

### Common Issues

**Issue**: CDK bootstrap fails
```bash
# Solution: Manually bootstrap the region
cdk bootstrap aws://ACCOUNT-ID/REGION
```

**Issue**: Stack deletion fails due to non-empty S3 bucket
```bash
# Solution: The script automatically empties buckets, but if it fails:
aws s3 rm s3://bucket-name --recursive
```

**Issue**: Stack is stuck in UPDATE_ROLLBACK_FAILED
```bash
# Solution: Use the rollback script
./scripts/rollback-stack.sh -e ENV -s STACK
```

**Issue**: Circular dependency detected
```bash
# Solution: Review stack dependencies in StackDependencyManager
# and remove circular references
```

### Getting Help

For issues with the scripts:
1. Check the log files in `logs/` directory
2. Review CloudFormation events in AWS Console
3. Check CloudWatch Logs for application errors
4. Verify AWS credentials and permissions

## Environment Variables

The scripts use the following environment variables:

- `CDK_DEFAULT_ACCOUNT`: AWS account ID (set automatically)
- `CDK_DEFAULT_REGION`: AWS region (set automatically)
- `AWS_PROFILE`: AWS CLI profile to use (optional)
- `AWS_REGION`: Default AWS region (optional)

## Security Considerations

1. **Credentials**: Never commit AWS credentials to version control
2. **Permissions**: Use least privilege IAM policies
3. **Encryption**: Enable encryption for sensitive resources
4. **Logging**: Enable CloudTrail for audit logging
5. **Secrets**: Use AWS Secrets Manager for sensitive data

## Contributing

When adding new stacks:

1. Update `StackDependencyManager.ConfigureStandardDependencies()` with dependencies
2. Add appropriate deletion policies in CDK code
3. Test deployment, update, and deletion in dev environment
4. Document any special considerations

## License

This project is part of the AWS SAP-C02 Practice Infrastructure.
