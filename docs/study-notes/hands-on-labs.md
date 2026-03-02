# Hands-On Labs - SAP-C02 Practice

## Overview

These hands-on labs are designed to reinforce SAP-C02 exam concepts through practical exercises using the infrastructure in this repository.

## Lab Prerequisites

- AWS account with appropriate permissions
- AWS CLI configured
- CDK installed and bootstrapped
- Basic understanding of AWS services

## Lab 1: Multi-AZ High Availability

**Duration:** 45 minutes
**Difficulty:** Intermediate
**Exam Domain:** Domain 2 - Design for New Solutions

### Objectives
- Deploy a highly available web application
- Implement hea
figure health checks**
   - Review ALB health check settings
   - Configure ASG health check grace period
   - Set up CloudWatch alarms

4. **Test failover**
   ```bash
   # Simulate instance failure
   INSTANCE_ID=$(aws ec2 describe-instances --filters "Name=tag:aws:autoscaling:groupName,Values=*" --query 'Reservations[0].Instances[0].InstanceId' --output text)
   aws ec2 terminate-instances --instance-ids $INSTANCE_ID

   # Watch ASG replace the instance
   aws autoscaling describe-auto-scaling-groups --query 'AutoScalingGroups[0].Instances'
   ```

5. **Verify application availability**
   ```bash
   ALB_DNS=$(aws elbv2 describe-load-balancers --query 'LoadBalancers[0].DNSName' --output text)
   while true; do curl -s http://$ALB_DNS && echo " - $(date)"; sleep 2; done
   ```

### Expected Results
- Application remains available during instance failure
- ASG automatically replaces failed instance
- No downtime observed at ALB level

### Cleanup
```bash
cdk destroy AsgStack AlbStack VpcStack
```

## Lab 2: Disaster Recovery - Pilot Light

**Duration:** 60 minutes
**Difficulty:** Advanced
**Exam Domain:** Domain 3 - Continuous Improvement

### Objectives
- Implement Pilot Light DR strategy
- Configure cross-region replication
- Practice failover procedures

### Steps

1. **Deploy primary region infrastructure**
   ```bash
   export AWS_REGION=us-east-1
   cdk deploy VpcStack AuroraStack S3Stack --require-approval never
   ```

2. **Deploy DR region (Pilot Light)**
   ```bash
   export AWS_REGION=us-west-2
   cdk deploy PilotLightStack --require-approval never
   ```

3. **Verify data replication**
   ```bash
   # Check Aurora Global Database
   aws rds describe-global-clusters

   # Verify S3 replication
   aws s3api get-bucket-replication --bucket <primary-bucket>
   ```

4. **Simulate disaster**
   ```bash
   # Assume primary region is unavailable
   export AWS_REGION=us-west-2
   ```

5. **Activate DR environment**
   ```bash
   # Promote Aurora secondary to primary
   aws rds failover-global-cluster --global-cluster-identifier <cluster-id> --target-db-cluster-identifier <dr-cluster>

   # Scale up compute resources
   aws autoscaling set-desired-capacity --auto-scaling-group-name <asg-name> --desired-capacity 3
   ```

6. **Update DNS**
   ```bash
   # Update Route53 to point to DR region
   aws route53 change-resource-record-sets --hosted-zone-id <zone-id> --change-batch file://failover-dns.json
   ```

7. **Verify DR environment**
   ```bash
   # Test application in DR region
   curl http://<dr-alb-dns>

   # Verify database connectivity
   aws rds describe-db-clusters --region us-west-2
   ```

### Expected Results
- Data successfully replicated to DR region
- Application functional in DR region after activation
- RTO < 1 hour, RPO < 15 minutes

### Cleanup
```bash
cdk destroy PilotLightStack --region us-west-2
cdk destroy S3Stack AuroraStack VpcStack --region us-east-1
```

## Lab 3: Serverless Event-Driven Architecture

**Duration:** 45 minutes
**Difficulty:** Intermediate
**Exam Domain:** Domain 4 - Migration and Modernization

### Objectives
- Build event-driven architecture
- Implement Lambda functions
- Use EventBridge for event routing

### Steps

1. **Deploy serverless infrastructure**
   ```bash
   cdk deploy ServerlessStack EventBridgeStack DynamoDbStack --require-approval never
   ```

2. **Create event rule**
   ```bash
   aws events put-rule \
     --name order-processing \
     --event-pattern '{"source":["custom.orders"],"detail-type":["Order Placed"]}'
   ```

3. **Add Lambda target**
   ```bash
   aws events put-targets \
     --rule order-processing \
     --targets "Id"="1","Arn"="<lambda-arn>"
   ```

4. **Test event flow**
   ```bash
   # Send test event
   aws events put-events \
     --entries '[{
       "Source": "custom.orders",
       "DetailType": "Order Placed",
       "Detail": "{\"orderId\":\"12345\",\"amount\":99.99}"
     }]'
   ```

5. **Verify processing**
   ```bash
   # Check Lambda logs
   aws logs tail /aws/lambda/<function-name> --follow

   # Verify DynamoDB record
   aws dynamodb get-item \
     --table-name orders \
     --key '{"orderId":{"S":"12345"}}'
   ```

6. **Monitor with X-Ray**
   ```bash
   # View service map
   aws xray get-service-graph --start-time <timestamp> --end-time <timestamp>
   ```

### Expected Results
- Events successfully routed to Lambda
- Data persisted in DynamoDB
- X-Ray traces show complete flow

### Cleanup
```bash
cdk destroy EventBridgeStack ServerlessStack DynamoDbStack
```

## Lab 4: Container Orchestration with ECS

**Duration:** 60 minutes
**Difficulty:** Intermediate
**Exam Domain:** Domain 4 - Migration and Modernization

### Objectives
- Deploy containerized application on ECS
- Implement auto-scaling
- Configure service mesh with App Mesh

### Steps

1. **Deploy ECS infrastructure**
   ```bash
   cdk deploy VpcStack EcsStack AlbStack --require-approval never
   ```

2. **Build and push container image**
   ```bash
   # Build Docker image
   docker build -t my-app:latest .

   # Tag for ECR
   docker tag my-app:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/my-app:latest

   # Push to ECR
   aws ecr get-login-password | docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com
   docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/my-app:latest
   ```

3. **Create ECS task definition**
   ```bash
   aws ecs register-task-definition --cli-input-json file://task-definition.json
   ```

4. **Deploy ECS service**
   ```bash
   aws ecs create-service \
     --cluster <cluster-name> \
     --service-name my-app \
     --task-definition my-app:1 \
     --desired-count 2 \
     --launch-type FARGATE \
     --network-configuration "awsvpcConfiguration={subnets=[<subnet-ids>],securityGroups=[<sg-id>]}" \
     --load-balancers "targetGroupArn=<tg-arn>,containerName=my-app,containerPort=80"
   ```

5. **Configure auto-scaling**
   ```bash
   # Register scalable target
   aws application-autoscaling register-scalable-target \
     --service-namespace ecs \
     --resource-id service/<cluster>/<service> \
     --scalable-dimension ecs:service:DesiredCount \
     --min-capacity 2 \
     --max-capacity 10

   # Create scaling policy
   aws application-autoscaling put-scaling-policy \
     --service-namespace ecs \
     --resource-id service/<cluster>/<service> \
     --scalable-dimension ecs:service:DesiredCount \
     --policy-name cpu-scaling \
     --policy-type TargetTrackingScaling \
     --target-tracking-scaling-policy-configuration file://scaling-policy.json
   ```

6. **Test application**
   ```bash
   ALB_DNS=$(aws elbv2 describe-load-balancers --query 'LoadBalancers[0].DNSName' --output text)
   curl http://$ALB_DNS
   ```

7. **Monitor with Container Insights**
   ```bash
   # View metrics in CloudWatch
   aws cloudwatch get-metric-statistics \
     --namespace AWS/ECS \
     --metric-name CPUUtilization \
     --dimensions Name=ServiceName,Value=my-app Name=ClusterName,Value=<cluster> \
     --start-time <timestamp> \
     --end-time <timestamp> \
     --period 300 \
     --statistics Average
   ```

### Expected Results
- Container successfully deployed to ECS
- Auto-scaling responds to load changes
- Container Insights provides visibility

### Cleanup
```bash
aws ecs delete-service --cluster <cluster> --service my-app --force
cdk destroy AlbStack EcsStack VpcStack
```

## Lab 5: Security - Encryption and Compliance

**Duration:** 45 minutes
**Difficulty:** Intermediate
**Exam Domain:** Domain 2 - Design for New Solutions

### Objectives
- Implement encryption at rest and in transit
- Configure KMS key policies
- Enable audit logging with CloudTrail

### Steps

1. **Deploy security infrastructure**
   ```bash
   cdk deploy KmsStack CloudTrailStack SecurityMonitoringStack --require-approval never
   ```

2. **Create customer-managed KMS key**
   ```bash
   aws kms create-key \
     --description "Application encryption key" \
     --key-policy file://key-policy.json
   ```

3. **Enable encryption for services**
   ```bash
   # S3 bucket encryption
   aws s3api put-bucket-encryption \
     --bucket <bucket-name> \
     --server-side-encryption-configuration '{
       "Rules": [{
         "ApplyServerSideEncryptionByDefault": {
           "SSEAlgorithm": "aws:kms",
           "KMSMasterKeyID": "<key-id>"
         }
       }]
     }'

   # RDS encryption
   aws rds create-db-instance \
     --db-instance-identifier encrypted-db \
     --storage-encrypted \
     --kms-key-id <key-id> \
     --engine postgres \
     --db-instance-class db.t3.medium
   ```

4. **Configure CloudTrail**
   ```bash
   # Verify CloudTrail is logging
   aws cloudtrail get-trail-status --name <trail-name>

   # Query recent events
   aws cloudtrail lookup-events --max-results 10
   ```

5. **Test encryption**
   ```bash
   # Upload encrypted object to S3
   aws s3 cp test.txt s3://<bucket>/test.txt

   # Verify encryption
   aws s3api head-object --bucket <bucket> --key test.txt
   ```

6. **Review audit logs**
   ```bash
   # Check CloudTrail logs
   aws s3 ls s3://<cloudtrail-bucket>/AWSLogs/<account-id>/CloudTrail/

   # Query with CloudWatch Insights
   aws logs start-query \
     --log-group-name /aws/cloudtrail \
     --start-time <timestamp> \
     --end-time <timestamp> \
     --query-string 'fields @timestamp, eventName, userIdentity.principalId | filter eventName = "PutObject"'
   ```

### Expected Results
- All data encrypted at rest with KMS
- CloudTrail logging all API calls
- Encryption keys properly managed

### Cleanup
```bash
cdk destroy SecurityMonitoringStack CloudTrailStack KmsStack
```

## Lab 6: Cost Optimization

**Duration:** 30 minutes
**Difficulty:** Beginner
**Exam Domain:** Domain 1 - Organizational Complexity

### Objectives
- Implement cost allocation tags
- Configure S3 lifecycle policies
- Set up billing alerts

### Steps

1. **Tag all resources**
   ```bash
   # Tag EC2 instances
   aws ec2 create-tags \
     --resources <instance-id> \
     --tags Key=Environment,Value=dev Key=Project,Value=SAP-C02 Key=CostCenter,Value=training

   # Tag S3 buckets
   aws s3api put-bucket-tagging \
     --bucket <bucket-name> \
     --tagging 'TagSet=[{Key=Environment,Value=dev},{Key=Project,Value=SAP-C02}]'
   ```

2. **Configure S3 lifecycle policies**
   ```bash
   aws s3api put-bucket-lifecycle-configuration \
     --bucket <bucket-name> \
     --lifecycle-configuration file://lifecycle.json
   ```

   lifecycle.json:
   ```json
   {
     "Rules": [{
       "Id": "Move to IA after 30 days",
       "Status": "Enabled",
       "Transitions": [{
         "Days": 30,
         "StorageClass": "STANDARD_IA"
       }, {
         "Days": 90,
         "StorageClass": "GLACIER"
       }],
       "Expiration": {
         "Days": 365
       }
     }]
   }
   ```

3. **Create billing alert**
   ```bash
   # Create SNS topic
   aws sns create-topic --name billing-alerts

   # Subscribe to topic
   aws sns subscribe \
     --topic-arn <topic-arn> \
     --protocol email \
     --notification-endpoint your-email@example.com

   # Create CloudWatch alarm
   aws cloudwatch put-metric-alarm \
     --alarm-name billing-alert \
     --alarm-description "Alert when charges exceed $100" \
     --metric-name EstimatedCharges \
     --namespace AWS/Billing \
     --statistic Maximum \
     --period 21600 \
     --evaluation-periods 1 \
     --threshold 100 \
     --comparison-operator GreaterThanThreshold \
     --alarm-actions <sns-topic-arn>
   ```

4. **Review cost reports**
   ```bash
   # Get cost and usage
   aws ce get-cost-and-usage \
     --time-period Start=2024-01-01,End=2024-01-31 \
     --granularity MONTHLY \
     --metrics BlendedCost \
     --group-by Type=TAG,Key=Environment
   ```

### Expected Results
- Resources properly tagged for cost allocation
- S3 lifecycle policies reducing storage costs
- Billing alerts configured

## Lab 7: Monitoring and Troubleshooting

**Duration:** 45 minutes
**Difficulty:** Intermediate
**Exam Domain:** Domain 3 - Continuous Improvement

### Objectives
- Create CloudWatch dashboards
- Configure alarms
- Use X-Ray for troubleshooting

### Steps

1. **Deploy monitoring infrastructure**
   ```bash
   cdk deploy MonitoringStack XRayStack --require-approval never
   ```

2. **Create custom dashboard**
   ```bash
   aws cloudwatch put-dashboard \
     --dashboard-name application-metrics \
     --dashboard-body file://dashboard.json
   ```

3. **Configure alarms**
   ```bash
   # CPU alarm
   aws cloudwatch put-metric-alarm \
     --alarm-name high-cpu \
     --alarm-description "Alert on high CPU" \
     --metric-name CPUUtilization \
     --namespace AWS/EC2 \
     --statistic Average \
     --period 300 \
     --evaluation-periods 2 \
     --threshold 80 \
     --comparison-operator GreaterThanThreshold \
     --alarm-actions <sns-topic-arn>
   ```

4. **Generate load for testing**
   ```bash
   # Use Apache Bench
   ab -n 10000 -c 100 http://<alb-dns>/
   ```

5. **Analyze with X-Ray**
   ```bash
   # Get service graph
   aws xray get-service-graph \
     --start-time $(date -u -d '1 hour ago' +%s) \
     --end-time $(date -u +%s)

   # Get trace summaries
   aws xray get-trace-summaries \
     --start-time $(date -u -d '1 hour ago' +%s) \
     --end-time $(date -u +%s)
   ```

6. **Use CloudWatch Logs Insights**
   ```bash
   aws logs start-query \
     --log-group-name /aws/lambda/<function-name> \
     --start-time $(date -u -d '1 hour ago' +%s) \
     --end-time $(date -u +%s) \
     --query-string 'fields @timestamp, @message | filter @message like /ERROR/ | sort @timestamp desc | limit 20'
   ```

### Expected Results
- Dashboard showing key metrics
- Alarms triggering on thresholds
- X-Ray traces identifying bottlenecks

### Cleanup
```bash
cdk destroy XRayStack MonitoringStack
```

## Additional Practice Scenarios

### Scenario 1: Design Multi-Region Architecture
- Deploy infrastructure in two regions
- Configure Route53 for failover
- Test regional failover

### Scenario 2: Implement Zero-Downtime Deployment
- Use blue/green deployment with ECS
- Configure weighted target groups
- Gradually shift traffic

### Scenario 3: Secure API with WAF
- Deploy API Gateway
- Configure WAF rules
- Test rate limiting and IP blocking

### Scenario 4: Implement Data Lake
- Set up S3 data lake
- Configure Glue crawlers
- Query with Athena

## Next Steps

1. Complete all labs in sequence
2. Experiment with variations
3. Practice troubleshooting failures
4. Review [Study Guide](README.md)
5. Take practice exams
