# AWS SAP-C02 Practice Infrastructure - Integration Guide

## Overview

This guide explains how the AWS SAP-C02 Practice Infrastructure components are integrated and wired together. The infrastructure is deployed in 8 phases with proper dependency management and cross-stack references.

## Architecture

The infrastructure consists of 30 stacks organized into 8 deployment phases:

### Phase 1: Core Infrastructure
- **VPC Stack**: Multi-AZ VPC with public, private, and isolated subnets
- **Transit Gateway Stack**: Connects multiple VPCs for hybrid connectivity
- **VPN Stack**: Site-to-Site VPN
ora Stack**: Aurora Global Database
- **ElastiCache Stack**: Redis cluster with Multi-AZ
- **DynamoDB Stack**: Global tables with encryption

### Phase 5: Compute Infrastructure
- **ALB Stack**: Application Load Balancer
- **ASG Stack**: Auto Scaling Groups
- **ECS Stack**: Fargate cluster
- **EKS Stack**: Managed Kubernetes cluster
- **App Mesh Stack**: Service mesh for microservices

### Phase 6: Serverless Infrastructure
- **Serverless Stack**: Lambda functions
- **API Gateway Stack**: REST and HTTP APIs
- **Step Functions Stack**: Workflow orchestration
- **EventBridge Stack**: Event-driven architecture

### Phase 7: Disaster Recovery
- **Backup Stack**: AWS Backup plans
- **Pilot Light Stack**: Minimal DR infrastructure
- **Warm Standby Stack**: Scaled-down DR environment
- **Route 53 Stack**: DNS failover and health checks

### Phase 8: Monitoring and Observability
- **Monitoring Stack**: CloudWatch dashboards and alarms
- **CloudWatch Logs Stack**: Centralized logging
- **X-Ray Stack**: Distributed tracing
- **Container Insights Stack**: Container monitoring

## Environment Configuration

The infrastructure supports three environments: **dev**, **staging**, and **prod**.

### Setting the Environment

You can specify the environment in three ways:

1. **CDK Context** (Recommended):
```bash
cdk deploy --context environment=dev
```

2. **Environment Variable**:
```bash
export ENVIRONMENT=staging
cdk deploy
```

3. **Default**: If not specified, defaults to `dev`

### Environment-Specific Settings

Each environment has different resource sizing and cost settings:

#### Development (dev)
- **Purpose**: Testing and development
- **RDS**: db.t3.medium
- **ECS**: 256 CPU, 512 MB memory
- **EKS**: t3.medium nodes, 1-3 nodes
- **Multi-AZ**: Disabled
- **Monthly Budget**: $100
- **Auto-Shutdown**: Enabled (8 PM daily)

#### Staging (staging)
- **Purpose**: Pre-production testing
- **RDS**: db.r5.large
- **ECS**: 512 CPU, 1024 MB memory
- **EKS**: t3.large nodes, 2-5 nodes
- **Multi-AZ**: Enabled
- **Monthly Budget**: $500
- **Auto-Shutdown**: Disabled

#### Production (prod)
- **Purpose**: Production workloads
- **RDS**: db.r5.xlarge
- **ECS**: 1024 CPU, 2048 MB memory
- **EKS**: m5.xlarge nodes, 3-10 nodes
- **Multi-AZ**: Enabled
- **Monthly Budget**: $2000
- **Auto-Shutdown**: Disabled

## Deployment

### Prerequisites

1. AWS CLI configured with appropriate credentials
2. .NET SDK 6.0 or higher
3. AWS CDK CLI installed: `npm install -g aws-cdk`
4. AWS account with appropriate permissions

### Deployment Steps

1. **Bootstrap CDK** (first time only):
```bash
cdk bootstrap aws://ACCOUNT-ID/us-east-1
cdk bootstrap aws://ACCOUNT-ID/eu-west-1
```

2. **Synthesize CloudFormation templates**:
```bash
cdk synth --context environment=dev
```

3. **Deploy all stacks**:
```bash
cdk deploy --all --context environment=dev
```

4. **Deploy specific stacks**:
```bash
cdk deploy aws-sap-c02-practice-dev-vpc --context environment=dev
```

### Deployment Order

The stacks are deployed with proper dependencies:

1. Core infrastructure (VPC, Transit Gateway, VPN)
2. Security infrastructure (KMS, WAF, CloudTrail)
3. Storage infrastructure (S3, CloudFront)
4. Database infrastructure (RDS, Aurora, ElastiCache, DynamoDB)
5. Compute infrastructure (ALB, ASG, ECS, EKS)
6. Serverless infrastructure (Lambda, API Gateway, Step Functions)
7. Disaster recovery (Backup, Pilot Light, Warm Standby, Route 53)
8. Monitoring and observability (CloudWatch, X-Ray, Container Insights)

## Integration Points

### VPC and Transit Gateway

The Transit Gateway connects all VPCs for centralized routing:

```csharp
integrationManager.WireVpcsWithTransitGateway(vpcStack, transitGatewayStack);
```

This automatically:
- Attaches VPCs to the Transit Gateway
- Configures route tables
- Sets up cross-VPC communication

### Monitoring Integration

All resources are automatically integrated with monitoring:

```csharp
integrationManager.WireMonitoringWithResources(monitoringStack);
```

This enables:
- CloudWatch metrics collection
- Alarm notifications
- Dashboard visualization
- Log aggregation

### Security Integration

Security is integrated across all stacks:
- All S3 buckets use KMS encryption
- All databases use KMS encryption
- CloudTrail logs all API calls
- WAF protects CloudFront and API Gateway
- Security Hub aggregates findings

## Parameter Store

Environment-specific configuration is stored in AWS Systems Manager Parameter Store:

### Parameters

- `/aws-sap-c02/{env}/db-name`: Database name
- `/aws-sap-c02/{env}/alarm-email`: Email for alarm notifications
- `/aws-sap-c02/{env}/log-retention-days`: Log retention period

### Creating Parameters

Parameters are automatically created during deployment. You can also create them manually:

```bash
aws ssm put-parameter \
  --name "/aws-sap-c02/dev/alarm-email" \
  --value "your-email@example.com" \
  --type String
```

## Stack Dependencies

Dependencies are explicitly defined to ensure proper deployment order:

```csharp
cloudTrailStack.AddDependency(kmsStack);
s3Stack.AddDependency(kmsStack);
rdsStack.AddDependency(vpcStack);
rdsStack.AddDependency(kmsStack);
```

## Cross-Stack References

Stacks share resources through CloudFormation exports:

- VPC IDs and CIDR blocks
- Security Group IDs
- Load Balancer ARNs
- Database endpoints
- S3 bucket names

## Cleanup

To avoid ongoing charges, destroy all stacks:

```bash
cdk destroy --all --context environment=dev
```

**Note**: Some resources may require manual deletion:
- S3 buckets with versioning enabled
- RDS snapshots
- CloudWatch log groups with retention policies

## Cost Optimization

### Development Environment

- Use Spot Instances for EKS nodes
- Enable auto-shutdown for non-production hours
- Use smaller instance types
- Disable Multi-AZ for databases

### Production Environment

- Use Savings Plans for predictable workloads
- Enable S3 Intelligent-Tiering
- Use Reserved Instances for baseline capacity
- Implement lifecycle policies for logs and backups

## Troubleshooting

### Stack Deployment Failures

1. Check CloudFormation console for error messages
2. Verify IAM permissions
3. Check resource limits (VPC limits, EIP limits, etc.)
4. Review CloudWatch Logs for Lambda errors

### Cross-Stack Reference Errors

If you see errors about missing exports:
1. Ensure dependent stacks are deployed first
2. Check that stack names match the expected format
3. Verify the environment parameter is consistent

### Resource Conflicts

If resources already exist:
1. Use unique project names in configuration
2. Check for leftover resources from previous deployments
3. Manually delete conflicting resources

## Best Practices

1. **Always use environment context**: Specify `--context environment=<env>` in all CDK commands
2. **Deploy to dev first**: Test changes in dev before deploying to staging/prod
3. **Review change sets**: Use `cdk diff` before deploying changes
4. **Tag all resources**: Tags are automatically applied for cost tracking
5. **Monitor costs**: Set up budget alerts for each environment
6. **Regular backups**: Verify backup plans are running successfully
7. **Test DR procedures**: Regularly test failover to DR regions

## Additional Resources

- [AWS CDK Documentation](https://docs.aws.amazon.com/cdk/)
- [AWS Well-Architected Framework](https://aws.amazon.com/architecture/well-architected/)
- [AWS SAP-C02 Exam Guide](https://aws.amazon.com/certification/certified-solutions-architect-professional/)

## Support

For issues or questions:
1. Check the CloudFormation console for detailed error messages
2. Review CloudWatch Logs for application errors
3. Consult AWS documentation for service-specific issues
