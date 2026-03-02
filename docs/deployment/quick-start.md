# Quick Start Guide

Get up and running with the AWS SAP-C02 Practice Infrastructure in under 30 minutes.

## Prerequisites Check

```bash
# Check AWS CLI
aws --version  # Should be 2.x+

# Check CDK
cdk --version  # Should be 2.x+

# Check .NET
dotnet --version  # Should be 6.0+

# Check AWS credentials
aws sts get-caller-identity
```

## 5-Minute Setup (Minimal)

Deploy only the essential components for quick testing:

```bash
# 1. Bootstrap CDK
cdk bootstrap

# 2. Deploy foundation
cdk deploy VpcStack --require-approval never

# 3. Deploy compute
cdk deploy EcsStack --require-approval never

# 4. Deploy load balancer
cdk deploy AlbStack --require-approval never

# 5. Deploy monitoring
cdk deploy MonitoringStack --require-approval never
```

## 15-Minute Setup (Standard)

Deploy a functional application stack:

```bash
# Deploy in order
cdk deploy VpcStack KmsStack S3Stack --require-approval never
cdk deploy AuroraStack ElastiCacheStack --require-approval never
cdk deploy EcsStack AlbStack --require-approval never
cdk deploy ApiGatewayStack ServerlessStack --require-approval never
cdk deploy MonitoringStack --require-approval never
```

## 30-Minute Setup (Complete)

Deploy the full infrastructure:

```bash
# Option 1: Deploy all at once
cdk deploy --all --require-approval never

# Option 2: Use deployment script
./scripts/deploy-stack.sh --environment dev --region us-east-1
```

## Verify Deployment

```bash
# Check stack status
aws cloudformation list-stacks --stack-status-filter CREATE_COMPLETE

# Get ALB endpoint
aws elbv2 describe-load-balancers --query 'LoadBalancers[0].DNSName' --output text

# Test endpoint
curl http://<alb-dns-name>
```

## Access Resources

### ECS Cluster
```bash
aws ecs list-clusters
aws ecs list-services --cluster <cluster-name>
```

### Aurora Database
```bash
aws rds describe-db-clusters --query 'DBClusters[0].Endpoint'
```

### CloudWatch Dashboard
```bash
aws cloudwatch list-dashboards
# Open in console: https://console.aws.amazon.com/cloudwatch/
```

## Common First Steps

### 1. Deploy a Sample Application
```bash
# Update ECS task definition with your container image
# Deploy updated service
aws ecs update-service --cluster <cluster> --service <service> --force-new-deployment
```

### 2. Configure DNS
```bash
# Create Route53 record pointing to ALB
aws route53 change-resource-record-sets --hosted-zone-id <zone-id> --change-batch file://dns-record.json
```

### 3. Enable HTTPS
```bash
# Request ACM certificate
aws acm request-certificate --domain-name example.com --validation-method DNS

# Add certificate to ALB listener
aws elbv2 modify-listener --listener-arn <arn> --certificates CertificateArn=<cert-arn>
```

## Cleanup

```bash
# Destroy all resources
cdk destroy --all

# Or use cleanup script
./scripts/delete-stack.sh --environment dev
```

## Next Steps

- [Full Deployment Guide](README.md)
- [Architecture Overview](../architecture/README.md)
- [Cost Estimation](../cost/README.md)
- [Troubleshooting](README.md#troubleshooting)
