# Quick Start Guide - AWS SAP-C02 Practice Infrastructure

## TL;DR

```bash
# 1. Bootstrap CDK (first time only)
./scripts/deploy.sh bootstrap -r us-east-1
./scripts/deploy.sh bootstrap -r eu-west-1

# 2. Deploy to dev environment
./scripts/deploy.sh deploy -e dev

# 3. Destroy when done
./scripts/deploy.sh destroy -e dev
```

## What Gets Deployed

This infrastructure deploys **30 stacks** across **8 phases**:

1. **Core**: VPC, Transit Gateway, VPN
2. **Security**: KMS, WAF, CloudTrail, Security Hub
3. **Storage**: S3, CloudFront
4. **Database**: RDS, Aurora, ElastiCache, DynamoDB
o |

## Common Commands

### Deploy Specific Stack
```bash
./scripts/deploy.sh deploy -e dev -s aws-sap-c02-practice-dev-vpc
```

### Show Changes Before Deploy
```bash
./scripts/deploy.sh diff -e dev
```

### Synthesize Templates
```bash
./scripts/deploy.sh synth -e dev
```

### Deploy to Production
```bash
./scripts/deploy.sh deploy -e prod
```

## Environment Variables

Set these before deployment (optional):

```bash
export ENVIRONMENT=dev
export CDK_DEFAULT_ACCOUNT=123456789012
export CDK_DEFAULT_REGION=us-east-1
```

## Parameter Store Values

After deployment, update these parameters:

```bash
# Set alarm email
aws ssm put-parameter \
  --name "/aws-sap-c02/dev/alarm-email" \
  --value "your-email@example.com" \
  --type String \
  --overwrite

# Set database name
aws ssm put-parameter \
  --name "/aws-sap-c02/dev/db-name" \
  --value "mydb" \
  --type String \
  --overwrite
```

## Stack Dependencies

Stacks are deployed in order with automatic dependency management:

```
VPC → Transit Gateway → VPN
  ↓
KMS → S3, RDS, Aurora, DynamoDB
  ↓
ALB → ASG
  ↓
ECS, EKS → Container Insights
  ↓
Monitoring (wired to all resources)
```

## Cost Optimization Tips

### For Development
- Deploy only needed stacks
- Use auto-shutdown feature
- Destroy when not in use
- Use smaller instance types

### Example: Deploy Only Core + Database
```bash
# Deploy VPC
./scripts/deploy.sh deploy -e dev -s aws-sap-c02-practice-dev-vpc

# Deploy RDS
./scripts/deploy.sh deploy -e dev -s aws-sap-c02-practice-dev-rds
```

## Troubleshooting

### "Stack already exists"
```bash
# Destroy and redeploy
./scripts/deploy.sh destroy -e dev -s STACK_NAME
./scripts/deploy.sh deploy -e dev -s STACK_NAME
```

### "Insufficient permissions"
Ensure your IAM user/role has:
- AdministratorAccess (for learning)
- Or specific permissions for all AWS services used

### "Resource limit exceeded"
Check AWS service quotas:
```bash
aws service-quotas list-service-quotas --service-code vpc
```

## Next Steps

1. **Review the architecture**: Check `docs/INTEGRATION_GUIDE.md`
2. **Customize configuration**: Edit `src/Models/EnvironmentConfig.cs`
3. **Add custom stacks**: Create new stacks in `src/Stacks/`
4. **Practice SAP-C02 scenarios**: Use the deployed infrastructure

## Important Notes

⚠️ **Cost Warning**: Deploying all stacks will incur AWS charges. Monitor your costs!

⚠️ **Cleanup**: Always destroy resources when done to avoid ongoing charges.

⚠️ **Multi-Region**: Some stacks deploy to secondary region (eu-west-1). Bootstrap both regions!

## Support

- **Documentation**: See `docs/INTEGRATION_GUIDE.md`
- **Issues**: Check CloudFormation console for detailed errors
- **Logs**: Check CloudWatch Logs for application errors

## Learning Resources

This infrastructure covers SAP-C02 exam topics:
- Domain 1: Design for Organizational Complexity
- Domain 2: Design for New Solutions
- Domain 3: Continuous Improvement for Existing Solutions
- Domain 4: Accelerate Workload Migration and Modernization

Practice scenarios:
1. Multi-region failover with Route 53
2. Disaster recovery testing
3. Cost optimization strategies
4. Security best practices
5. Monitoring and observability
6. Hybrid cloud connectivity
7. Microservices architecture
8. Serverless patterns
