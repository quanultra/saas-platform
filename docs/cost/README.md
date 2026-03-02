# Cost Estimation Guide

## Overview

This document provides detailed cost estimates for the AWS SAP-C02 Practice Infrastructure. Costs are estimates based on US East (N. Virginia) region pricing as of 2024.

⚠️ **Important**: Actual costs may vary based on:
- Usage patterns
- Data transfer
- Region selection
- Reserved instances or Savings Plans
- AWS Free Tier eligibility

## Monthly Cost Summary

| Configuration | Estimated Monthly Cost |
|--------------|----------------------|
| Minimal (Development) | $150 - $300 |
| Standard (Testing) | $500 - $800 |
| Complete (Production-like) | $1,500 - $2,500 |
| Complete + DR | $2,500 - $4,000 |

## Detailed Cost Breakdown

### 1. Network Infrastructure

#### VPC
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| VPC | 1-2 | Free | $0 |
| NAT Gateway | 2-4 | $0.045/hour | $65-$130 |
| NAT Gateway Data | 1TB | $0.045/GB | $45 |
| VPC Flow Logs | 100GB | $0.50/GB | $50 |
| **Subtotal** | | | **$160-$225** |

#### Transit Gateway (Optional)
| Resource | Quantity | Unit Cost | Monthly Cost
Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| ECS Cluster | 1 | Free | $0 |
| EC2 Instances (t3.medium) | 3 | $0.0416/hour | $90 |
| EBS Volumes (gp3) | 300GB | $0.08/GB | $24 |
| **Subtotal** | | | **$114** |

#### EKS (Optional)
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| EKS Cluster | 1 | $0.10/hour | $73 |
| EC2 Nodes (t3.medium) | 3 | $0.0416/hour | $90 |
| EBS Volumes (gp3) | 300GB | $0.08/GB | $24 |
| **Subtotal** | | | **$187** |

#### Auto Scaling Group
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| EC2 Instances (t3.medium) | 2-6 | $0.0416/hour | $60-$180 |
| EBS Volumes (gp3) | 200GB | $0.08/GB | $16 |
| **Subtotal** | | | **$76-$196** |

#### Lambda
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Requests | 10M | $0.20/1M | $2 |
| Duration (128MB) | 1M GB-seconds | $0.0000166667 | $17 |
| **Subtotal** | | | **$19** |

### 3. Database & Storage

#### Aurora Global Database
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Primary Instance (db.r6g.large) | 1 | $0.29/hour | $211 |
| Read Replica | 1 | $0.29/hour | $211 |
| Storage (gp3) | 100GB | $0.10/GB | $10 |
| I/O Operations | 10M | $0.20/1M | $2 |
| Backup Storage | 100GB | $0.021/GB | $2 |
| Cross-Region Replication | 50GB | $0.09/GB | $5 |
| **Subtotal** | | | **$441** |

#### RDS (Alternative)
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Instance (db.t3.medium) | 1 | $0.068/hour | $50 |
| Storage (gp3) | 100GB | $0.115/GB | $12 |
| Backup Storage | 100GB | $0.095/GB | $10 |
| **Subtotal** | | | **$72** |

#### DynamoDB
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| On-Demand Writes | 10M | $1.25/1M | $13 |
| On-Demand Reads | 30M | $0.25/1M | $8 |
| Storage | 25GB | $0.25/GB | $6 |
| Global Tables Replication | 5GB | $1.875/GB | $9 |
| **Subtotal** | | | **$36** |

#### ElastiCache (Redis)
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Node (cache.t3.medium) | 2 | $0.068/hour | $99 |
| Backup Storage | 10GB | $0.085/GB | $1 |
| **Subtotal** | | | **$100** |

#### S3
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Standard Storage | 1TB | $0.023/GB | $23 |
| Requests (PUT) | 1M | $0.005/1K | $5 |
| Requests (GET) | 10M | $0.0004/1K | $4 |
| Data Transfer Out | 500GB | $0.09/GB | $45 |
| Cross-Region Replication | 100GB | $0.02/GB | $2 |
| **Subtotal** | | | **$79** |

### 4. Load Balancing & CDN

#### Application Load Balancer
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| ALB | 1 | $0.0225/hour | $16 |
| LCU Hours | 730 | $0.008/LCU-hour | $6 |
| **Subtotal** | | | **$22** |

#### CloudFront
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Data Transfer Out | 1TB | $0.085/GB | $85 |
| Requests | 10M | $0.0075/10K | $8 |
| **Subtotal** | | | **$93** |

#### Route53
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Hosted Zone | 1 | $0.50/month | $1 |
| Queries | 1M | $0.40/1M | $0.40 |
| Health Checks | 2 | $0.50/check | $1 |
| **Subtotal** | | | **$2.40** |

### 5. Security & Compliance

#### KMS
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Customer Managed Keys | 5 | $1/key | $5 |
| API Requests | 100K | $0.03/10K | $0.30 |
| **Subtotal** | | | **$5.30** |

#### WAF
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Web ACL | 1 | $5/month | $5 |
| Rules | 10 | $1/rule | $10 |
| Requests | 10M | $0.60/1M | $6 |
| **Subtotal** | | | **$21** |

#### CloudTrail
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Management Events | 1 trail | Free | $0 |
| Data Events | 1M | $0.10/100K | $1 |
| S3 Storage | 50GB | $0.023/GB | $1 |
| **Subtotal** | | | **$2** |

#### AWS Config
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Configuration Items | 10K | $0.003/item | $30 |
| Rules | 10 | $2/rule | $20 |
| **Subtotal** | | | **$50** |

### 6. Monitoring & Observability

#### CloudWatch
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Custom Metrics | 100 | $0.30/metric | $30 |
| API Requests | 1M | $0.01/1K | $10 |
| Logs Ingestion | 100GB | $0.50/GB | $50 |
| Logs Storage | 100GB | $0.03/GB | $3 |
| Dashboards | 3 | $3/dashboard | $9 |
| Alarms | 50 | $0.10/alarm | $5 |
| **Subtotal** | | | **$107** |

#### X-Ray
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Traces Recorded | 1M | $5/1M | $5 |
| Traces Retrieved | 100K | $0.50/1M | $0.05 |
| **Subtotal** | | | **$5.05** |

#### Container Insights
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Metrics | 50 | $0.30/metric | $15 |
| Logs | 50GB | $0.50/GB | $25 |
| **Subtotal** | | | **$40** |

### 7. Integration Services

#### API Gateway
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| REST API Requests | 10M | $3.50/1M | $35 |
| Cache (0.5GB) | 730 hours | $0.02/hour | $15 |
| **Subtotal** | | | **$50** |

#### EventBridge
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Custom Events | 10M | $1/1M | $10 |
| Cross-Region Events | 1M | $0.01/event | $10 |
| **Subtotal** | | | **$20** |

#### Step Functions
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| State Transitions | 1M | $25/1M | $25 |
| **Subtotal** | | | **$25** |

#### App Mesh (Optional)
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Envoy Proxy Instances | 10 | $0.025/hour | $18 |
| **Subtotal** | | | **$18** |

### 8. Disaster Recovery

#### AWS Backup
| Resource | Quantity | Unit Cost | Monthly Cost |
|----------|----------|-----------|--------------|
| Backup Storage | 500GB | $0.05/GB | $25 |
| Restore Requests | 10 | $0.02/GB | $1 |
| **Subtotal** | | | **$26** |

#### Pilot Light (DR Region)
| Resource | Description | Monthly Cost |
|----------|-------------|--------------|
| Data Replication | Aurora, S3, DynamoDB | $50 |
| Minimal Compute | Stopped instances | $10 |
| **Subtotal** | | **$60** |

#### Warm Standby (DR Region)
| Resource | Description | Monthly Cost |
|----------|-------------|--------------|
| Scaled-Down Infrastructure | 30% of primary | $500 |
| Data Replication | Real-time sync | $100 |
| **Subtotal** | | **$600** |

## Configuration-Based Estimates

### Minimal Configuration (Development)
```
VPC + NAT Gateway:           $160
ECS (1 instance):            $38
RDS (t3.small):              $30
S3 (100GB):                  $10
ALB:                         $22
CloudWatch (basic):          $30
KMS:                         $5
CloudTrail:                  $2
─────────────────────────────
Total:                       $297/month
```

### Standard Configuration (Testing)
```
VPC + NAT Gateway:           $160
ECS (3 instances):           $114
Aurora (1 primary):          $223
ElastiCache:                 $100
S3 (500GB):                  $40
ALB:                         $22
API Gateway:                 $50
Lambda:                      $19
CloudWatch:                  $107
X-Ray:                       $5
KMS:                         $5
WAF:                         $21
CloudTrail:                  $2
AWS Config:                  $50
─────────────────────────────
Total:                       $918/month
```

### Complete Configuration (Production-like)
```
VPC + NAT Gateway:           $225
Transit Gateway:             $200
ECS (3 instances):           $114
EKS:                         $187
ASG (4 instances):           $130
Aurora Global:               $441
DynamoDB:                    $36
ElastiCache:                 $100
S3 (1TB):                    $79
ALB:                         $22
CloudFront:                  $93
Route53:                     $2
API Gateway:                 $50
Lambda:                      $19
EventBridge:                 $20
Step Functions:              $25
App Mesh:                    $18
CloudWatch:                  $107
X-Ray:                       $5
Container Insights:          $40
KMS:                         $5
WAF:                         $21
CloudTrail:                  $2
AWS Config:                  $50
AWS Backup:                  $26
─────────────────────────────
Total:                       $2,017/month
```

### Complete + Disaster Recovery
```
Complete Configuration:      $2,017
Pilot Light (DR):            $60
Warm Standby (DR):           $600
Cross-Region Data Transfer:  $200
─────────────────────────────
Total:                       $2,877/month
```

## Cost Optimization Strategies

### 1. Right-Sizing
- Use t3/t4g instances for non-production
- Start with smaller instance types
- Monitor and adjust based on actual usage

### 2. Reserved Instances / Savings Plans
- 1-year commitment: 30-40% savings
- 3-year commitment: 50-60% savings
- Apply to: EC2, RDS, ElastiCache

### 3. Spot Instances
- Use for non-critical workloads
- 70-90% cost savings
- Apply to: ECS tasks, batch processing

### 4. Storage Optimization
- Use S3 Intelligent-Tiering
- Implement lifecycle policies
- Delete old snapshots and backups
- Use gp3 instead of gp2 for EBS

### 5. Data Transfer Optimization
- Use CloudFront for static content
- Minimize cross-region transfers
- Use VPC endpoints for AWS services
- Compress data before transfer

### 6. Monitoring & Cleanup
- Delete unused resources
- Stop non-production environments after hours
- Use AWS Cost Explorer
- Set up billing alerts

### 7. Serverless First
- Use Lambda instead of EC2 where possible
- Use DynamoDB on-demand for variable workloads
- Use API Gateway caching

### 8. Development Practices
- Use CDK destroy for test environments
- Implement auto-shutdown for dev resources
- Share non-production environments
- Use AWS Free Tier where applicable

## Cost Monitoring Setup

### 1. Enable Cost Explorer
```bash
aws ce get-cost-and-usage \
  --time-period Start=2024-01-01,End=2024-01-31 \
  --granularity MONTHLY \
  --metrics BlendedCost
```

### 2. Create Budget Alerts
```bash
aws budgets create-budget \
  --account-id 123456789012 \
  --budget file://budget.json \
  --notifications-with-subscribers file://notifications.json
```

### 3. Tag Resources
```typescript
Tags.of(stack).add('Environment', 'dev');
Tags.of(stack).add('Project', 'SAP-C02');
Tags.of(stack).add('CostCenter', 'training');
```

### 4. Use Cost Allocation Tags
- Enable in Billing Console
- Tag all resources consistently
- Generate cost reports by tag

## Monthly Cost Calculator

Use this template to estimate your specific costs:

```
Component                    | Quantity | Unit Cost | Total
─────────────────────────────|──────────|───────────|──────
VPC NAT Gateways            | ___      | $32.50    | $___
EC2 Instances (t3.medium)   | ___      | $30.00    | $___
Aurora (db.r6g.large)       | ___      | $211.00   | $___
ElastiCache (cache.t3.medium)| ___     | $49.50    | $___
S3 Storage (per TB)         | ___      | $23.00    | $___
ALB                         | ___      | $22.00    | $___
CloudWatch Logs (per 100GB) | ___      | $50.00    | $___
─────────────────────────────|──────────|───────────|──────
                                         Total:      $___
```

## Free Tier Considerations

If you're within the first 12 months of AWS account creation:

**Always Free:**
- Lambda: 1M requests/month
- DynamoDB: 25GB storage
- CloudWatch: 10 custom metrics
- CloudTrail: 1 trail

**12-Month Free:**
- EC2: 750 hours/month (t2.micro/t3.micro)
- RDS: 750 hours/month (db.t2.micro/db.t3.micro)
- S3: 5GB standard storage
- CloudFront: 50GB data transfer

## Next Steps

1. Review [Architecture Documentation](../architecture/README.md)
2. Plan your [Deployment Strategy](../deployment/README.md)
3. Set up [Cost Monitoring](../monitoring/cost-monitoring.md)
4. Implement [Cost Optimization](../optimization/README.md)
