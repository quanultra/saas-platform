# Deployment Guide - AWS SAP-C02 Practice Infrastructure

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Initial Setup](#initial-setup)
3. [Deployment Steps](#deployment-steps)
4. [Stack Dependencies](#stack-dependencies)
5. [Verification](#verification)
6. [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Tools
- **AWS CLI** (v2.x or later)
  ```bash
  aws --version
  ```
- **AWS CDK** (v2.x or later)
  ```bash
  npm install -g aws-cdk
  cdk --version
  ```
- **.NET SDK** (6.0 or later)
  ```bash
  dotnet --version
  ```
- **Node.js** (v16.x o
ort AWS_SECRET_ACCESS_KEY=your_secret_key
   export AWS_DEFAULT_REGION=us-east-1
   ```

### Cost Considerations
⚠️ **Warning**: This infrastructure will incur AWS charges. Review the [Cost Estimation Guide](../cost/README.md) before deployment.

Estimated monthly cost: $500-$2000 depending on usage and configuration.

## Initial Setup

### 1. Clone and Build
```bash
# Clone the repository
git clone <repository-url>
cd aws-sap-c02-practice-infrastructure

# Restore dependencies
dotnet restore

# Build the project
dotnet build
```

### 2. Bootstrap CDK
Bootstrap CDK in your target regions:

```bash
# Primary region
cdk bootstrap aws://ACCOUNT-ID/us-east-1

# Secondary region (for multi-region features)
cdk bootstrap aws://ACCOUNT-ID/us-west-2
```

### 3. Configure Environment
Create a configuration file or set environment variables:

```bash
# Set environment variables
export ENVIRONMENT=dev
export PRIMARY_REGION=us-east-1
export SECONDARY_REGION=us-west-2
export ENABLE_DR=true
```

## Deployment Steps

### Phase 1: Foundation (Core Infrastructure)

#### Step 1: Deploy VPC Stack
```bash
cdk deploy VpcStack --require-approval never
```

**What it creates:**
- VPC with public/private subnets
- NAT Gateways
- Internet Gateway
- Route tables
- VPC Flow Logs

**Verification:**
```bash
aws ec2 describe-vpcs --filters "Name=tag:Name,Values=*VpcStack*"
```

#### Step 2: Deploy KMS Stack
```bash
cdk deploy KmsStack --require-approval never
```

**What it creates:**
- KMS keys for encryption
- Key policies
- Key aliases

#### Step 3: Deploy Security Monitoring Stack
```bash
cdk deploy SecurityMonitoringStack --require-approval never
```

**What it creates:**
- CloudTrail
- AWS Config
- GuardDuty (optional)
- Security Hub (optional)

### Phase 2: Data Layer

#### Step 4: Deploy S3 Stack
```bash
cdk deploy S3Stack --require-approval never
```

**What it creates:**
- S3 buckets with versioning
- Lifecycle policies
- Cross-region replication (if enabled)
- Bucket policies

#### Step 5: Deploy Aurora Stack
```bash
cdk deploy AuroraStack --require-approval never
```

**What it creates:**
- Aurora cluster
- Read replicas
- Parameter groups
- Subnet groups

**Note:** This may take 10-15 minutes.

#### Step 6: Deploy DynamoDB Stack
```bash
cdk deploy DynamoDbStack --require-approval never
```

**What it creates:**
- DynamoDB tables
- Global tables (if multi-region)
- Auto-scaling policies
- Point-in-time recovery

#### Step 7: Deploy ElastiCache Stack
```bash
cdk deploy ElastiCacheStack --require-approval never
```

**What it creates:**
- Redis or Memcached cluster
- Subnet groups
- Parameter groups

### Phase 3: Compute Layer

#### Step 8: Deploy ECS Stack
```bash
cdk deploy EcsStack --require-approval never
```

**What it creates:**
- ECS cluster
- Task definitions
- Services
- Auto-scaling policies

#### Step 9: Deploy EKS Stack (Optional)
```bash
cdk deploy EksStack --require-approval never
```

**What it creates:**
- EKS cluster
- Node groups
- IAM roles
- kubectl configuration

**Note:** This may take 15-20 minutes.

#### Step 10: Deploy Auto Scaling Group Stack
```bash
cdk deploy AsgStack --require-approval never
```

**What it creates:**
- Launch templates
- Auto Scaling groups
- Scaling policies

### Phase 4: Application Layer

#### Step 11: Deploy ALB Stack
```bash
cdk deploy AlbStack --require-approval never
```

**What it creates:**
- Application Load Balancer
- Target groups
- Listener rules
- Health checks

#### Step 12: Deploy API Gateway Stack
```bash
cdk deploy ApiGatewayStack --require-approval never
```

**What it creates:**
- REST API
- Resources and methods
- API keys
- Usage plans

#### Step 13: Deploy Serverless Stack
```bash
cdk deploy ServerlessStack --require-approval never
```

**What it creates:**
- Lambda functions
- Event sources
- IAM roles
- Environment variables

### Phase 5: Integration & Orchestration

#### Step 14: Deploy EventBridge Stack
```bash
cdk deploy EventBridgeStack --require-approval never
```

**What it creates:**
- Event buses
- Event rules
- Event targets

#### Step 15: Deploy Step Functions Stack
```bash
cdk deploy StepFunctionsStack --require-approval never
```

**What it creates:**
- State machines
- IAM roles
- CloudWatch logs

### Phase 6: Edge & CDN

#### Step 16: Deploy CloudFront Stack
```bash
cdk deploy CloudFrontStack --require-approval never
```

**What it creates:**
- CloudFront distributions
- Origin configurations
- Cache behaviors
- SSL certificates

#### Step 17: Deploy Route53 Stack
```bash
cdk deploy Route53Stack --require-approval never
```

**What it creates:**
- Hosted zones
- DNS records
- Health checks
- Traffic policies

#### Step 18: Deploy WAF Stack
```bash
cdk deploy WafStack --require-approval never
```

**What it creates:**
- WAF web ACLs
- WAF rules
- IP sets
- Rate limiting

### Phase 7: Monitoring & Observability

#### Step 19: Deploy Monitoring Stack
```bash
cdk deploy MonitoringStack --require-approval never
```

**What it creates:**
- CloudWatch dashboards
- CloudWatch alarms
- SNS topics
- Metric filters

#### Step 20: Deploy X-Ray Stack
```bash
cdk deploy XRayStack --require-approval never
```

**What it creates:**
- X-Ray sampling rules
- X-Ray groups
- Service maps

#### Step 21: Deploy Container Insights Stack
```bash
cdk deploy ContainerInsightsStack --require-approval never
```

**What it creates:**
- Container Insights configuration
- Log groups
- Metric filters

### Phase 8: Disaster Recovery (Optional)

#### Step 22: Deploy Backup Stack
```bash
cdk deploy BackupStack --require-approval never
```

**What it creates:**
- Backup vaults
- Backup plans
- Backup selections
- Lifecycle policies

#### Step 23: Deploy Pilot Light Stack
```bash
cdk deploy PilotLightStack --require-approval never
```

**What it creates:**
- Minimal DR infrastructure
- Data replication
- Standby resources

#### Step 24: Deploy Warm Standby Stack
```bash
cdk deploy WarmStandbyStack --require-approval never
```

**What it creates:**
- Scaled-down production replica
- Active-passive configuration
- Failover mechanisms

### Phase 9: Multi-Region (Optional)

#### Step 25: Deploy Transit Gateway Stack
```bash
cdk deploy TransitGatewayStack --require-approval never
```

**What it creates:**
- Transit Gateway
- Transit Gateway attachments
- Route tables
- Peering connections

#### Step 26: Deploy VPN Stack
```bash
cdk deploy VpnStack --require-approval never
```

**What it creates:**
- Virtual Private Gateway
- Customer Gateway
- VPN connections
- VPN tunnels

## Deploy All Stacks

To deploy all stacks at once:

```bash
cdk deploy --all --require-approval never
```

⚠️ **Warning**: This will deploy all stacks and may take 1-2 hours. Monitor for any failures.

## Stack Dependencies

```
VpcStack (Foundation)
├── KmsStack
├── SecurityMonitoringStack
├── S3Stack
├── AuroraStack
│   └── ElastiCacheStack
├── DynamoDbStack
├── EcsStack
│   ├── AlbStack
│   └── AppMeshStack
├── EksStack
│   └── AlbStack
├── AsgStack
│   └── AlbStack
├── ApiGatewayStack
├── ServerlessStack
│   ├── EventBridgeStack
│   └── StepFunctionsStack
├── CloudFrontStack
│   ├── Route53Stack
│   └── WafStack
├── MonitoringStack
│   ├── XRayStack
│   └── ContainerInsightsStack
├── BackupStack
├── PilotLightStack
├── WarmStandbyStack
├── TransitGatewayStack
└── VpnStack
```

## Verification

### 1. Check Stack Status
```bash
# List all stacks
aws cloudformation list-stacks --stack-status-filter CREATE_COMPLETE

# Describe specific stack
aws cloudformation describe-stacks --stack-name VpcStack
```

### 2. Verify Resources

**VPC:**
```bash
aws ec2 describe-vpcs --filters "Name=tag:aws:cloudformation:stack-name,Values=VpcStack"
```

**ECS Cluster:**
```bash
aws ecs list-clusters
aws ecs describe-clusters --clusters <cluster-name>
```

**Aurora Database:**
```bash
aws rds describe-db-clusters
```

**S3 Buckets:**
```bash
aws s3 ls
```

### 3. Test Connectivity

**Test ALB:**
```bash
ALB_DNS=$(aws elbv2 describe-load-balancers --query 'LoadBalancers[0].DNSName' --output text)
curl http://$ALB_DNS
```

**Test API Gateway:**
```bash
API_URL=$(aws apigateway get-rest-apis --query 'items[0].id' --output text)
curl https://$API_URL.execute-api.us-east-1.amazonaws.com/prod/
```

### 4. Check Monitoring

**CloudWatch Dashboards:**
```bash
aws cloudwatch list-dashboards
```

**CloudWatch Alarms:**
```bash
aws cloudwatch describe-alarms
```

## Troubleshooting

### Common Issues

#### 1. CDK Bootstrap Error
```
Error: This stack uses assets, so the toolkit stack must be deployed
```

**Solution:**
```bash
cdk bootstrap aws://ACCOUNT-ID/REGION
```

#### 2. Insufficient Permissions
```
Error: User is not authorized to perform: cloudformation:CreateStack
```

**Solution:**
- Verify IAM permissions
- Ensure you have AdministratorAccess or equivalent

#### 3. Resource Limit Exceeded
```
Error: You have reached the limit for VPCs in this region
```

**Solution:**
- Request limit increase via AWS Support
- Or delete unused resources

#### 4. Stack Rollback
```
Stack CREATE_FAILED: Resource creation cancelled
```

**Solution:**
```bash
# Check stack events
aws cloudformation describe-stack-events --stack-name <stack-name>

# Delete failed stack
cdk destroy <stack-name>

# Retry deployment
cdk deploy <stack-name>
```

#### 5. Dependency Errors
```
Error: Resource <resource-id> does not exist
```

**Solution:**
- Deploy stacks in correct order (see Stack Dependencies)
- Ensure prerequisite stacks are deployed successfully

### Debug Commands

**View CDK Diff:**
```bash
cdk diff <stack-name>
```

**View Synthesized CloudFormation:**
```bash
cdk synth <stack-name> > template.yaml
```

**Enable CDK Debug Logging:**
```bash
cdk deploy <stack-name> --verbose
```

**Check CloudFormation Events:**
```bash
aws cloudformation describe-stack-events --stack-name <stack-name> --max-items 20
```

## Cleanup

To destroy all resources:

```bash
# Destroy specific stack
cdk destroy <stack-name>

# Destroy all stacks
cdk destroy --all
```

⚠️ **Warning**: This will delete all resources. Ensure you have backups if needed.

### Manual Cleanup

Some resources may require manual deletion:
- S3 buckets with objects
- RDS snapshots
- CloudWatch log groups
- ECR images

## Next Steps

1. Review [Cost Estimation](../cost/README.md)
2. Explore [Architecture Documentation](../architecture/README.md)
3. Study [SAP-C02 Exam Mapping](../study-notes/README.md)
4. Configure [Monitoring Dashboards](../monitoring/README.md)

## Support

For issues or questions:
- Check [Troubleshooting Guide](#troubleshooting)
- Review AWS CDK documentation
- Check CloudFormation stack events
