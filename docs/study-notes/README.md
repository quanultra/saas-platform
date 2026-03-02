# AWS SAP-C02 Exam Study Guide

## Overview

This guide maps the infrastructure components in this repository to the AWS Certified Solutions Architect - Professional (SAP-C02) exam domains and topics.

## Exam Domains

The SAP-C02 exam consists of four domains:

| Domain | Weight | Focus Areas |
|--------|--------|-------------|
| Domain 1: Design Solutions for Organizational Complexity | 26% | Multi-account, hybrid, cost optimization |
| Domain 2: Design for New Solutions | 29% | Security, reliability, performance, cost |
| Domain 3: Continuous Improvement for Existin
connectivity

**Practice Scenarios:**
```
Scenario 1: Design a multi-account strategy for a company with dev, test, and prod environments
- Use separate AWS accounts per environment
- Implement Transit Gateway for cross-account networking
- Configure cross-account IAM roles for deployment

Scenario 2: Implement centralized logging across multiple accounts
- Use CloudTrail with organization trail
- Centralize logs in security account S3 bucket
- Implement cross-account CloudWatch log aggregation
```

**Key Files:**
- `src/Stacks/VpcStack.cs` - Multi-VPC architecture
- `src/Constructs/Network/TransitGatewayConstruct.cs` - Cross-account networking
- `src/Stacks/CloudTrailStack.cs` - Centralized audit logging

### 1.2 Hybrid Connectivity

**Exam Topics:**
- AWS Direct Connect
- VPN connections
- Transit Gateway
- AWS PrivateLink

**Infrastructure Components:**
- Site-to-Site VPN
- Transit Gateway with VPN attachments
- VPC peering

**Practice Scenarios:**
```
Scenario 1: Connect on-premises data center to AWS
- Implement Site-to-Site VPN for initial connectivity
- Plan Direct Connect for production workloads
- Use Transit Gateway as central hub

Scenario 2: Design hybrid DNS resolution
- Configure Route53 Resolver endpoints
- Implement conditional forwarding
- Set up DNS query logging
```

**Key Files:**
- `src/Stacks/VpnStack.cs` - VPN connectivity
- `src/Stacks/TransitGatewayStack.cs` - Hub-and-spoke networking
- `src/Constructs/Network/SiteToSiteVpn.cs` - VPN implementation

### 1.3 Cost Optimization

**Exam Topics:**
- Cost allocation tags
- AWS Cost Explorer
- Reserved Instances
- Savings Plans
- Right-sizing

**Infrastructure Components:**
- Resource tagging strategy
- Auto Scaling policies
- S3 lifecycle policies

**Practice Scenarios:**
```
Scenario 1: Reduce costs for a variable workload
- Implement Auto Scaling for compute
- Use Spot Instances for batch processing
- Configure S3 Intelligent-Tiering

Scenario 2: Optimize database costs
- Use Aurora Serverless for variable workloads
- Implement read replicas for read-heavy workloads
- Use DynamoDB on-demand for unpredictable traffic
```

**Key Files:**
- `src/Stacks/AsgStack.cs` - Auto Scaling implementation
- `src/Stacks/S3Stack.cs` - Lifecycle policies
- `src/Models/StackConfiguration.cs` - Resource tagging

## Domain 2: Design for New Solutions (29%)

### 2.1 Security Design

**Exam Topics:**
- Encryption at rest and in transit
- KMS key management
- IAM policies and roles
- Security groups and NACLs
- WAF and Shield

**Infrastructure Components:**
- KMS encryption for all data stores
- WAF rules for application protection
- Security groups with least privilege
- CloudTrail for audit logging

**Practice Scenarios:**
```
Scenario 1: Design encryption strategy for sensitive data
- Use KMS customer-managed keys
- Enable encryption at rest for RDS, S3, EBS
- Implement TLS for data in transit
- Configure key rotation policies

Scenario 2: Implement defense in depth
- Layer 1: WAF at edge (CloudFront)
- Layer 2: Security groups at ALB
- Layer 3: Security groups at instance level
- Layer 4: Encryption at data layer
- Layer 5: Audit with CloudTrail
```

**Key Files:**
- `src/Stacks/KmsStack.cs` - Encryption key management
- `src/Stacks/WafStack.cs` - Web application firewall
- `src/Stacks/SecurityMonitoringStack.cs` - Security monitoring
- `src/Stacks/CloudTrailStack.cs` - Audit logging

### 2.2 High Availability Design

**Exam Topics:**
- Multi-AZ deployments
- Auto Scaling
- Load balancing
- Health checks
- Failover mechanisms

**Infrastructure Components:**
- Multi-AZ VPC with subnets
- Application Load Balancer
- Auto Scaling Groups
- Aurora with read replicas
- ElastiCache with replication

**Practice Scenarios:**
```
Scenario 1: Design highly available web application
- Deploy ALB across multiple AZs
- Use Auto Scaling Group with min 2 instances
- Implement health checks at ALB and ASG
- Use Aurora with Multi-AZ enabled

Scenario 2: Implement database high availability
- Use Aurora Global Database for multi-region
- Configure automatic failover
- Implement read replicas for read scaling
- Set up cross-region replication
```

**Key Files:**
- `src/Stacks/AlbStack.cs` - Load balancing
- `src/Stacks/AsgStack.cs` - Auto Scaling
- `src/Stacks/AuroraStack.cs` - HA database
- `src/Constructs/Database/AuroraGlobalDatabase.cs` - Global database

### 2.3 Performance Optimization

**Exam Topics:**
- Caching strategies
- CDN (CloudFront)
- Database optimization
- Compute optimization

**Infrastructure Components:**
- CloudFront for edge caching
- ElastiCache for application caching
- Aurora read replicas
- ECS with auto-scaling

**Practice Scenarios:**
```
Scenario 1: Optimize application performance
- Implement CloudFront for static content
- Use ElastiCache for session data
- Configure Aurora read replicas for read traffic
- Enable CloudFront compression

Scenario 2: Reduce database latency
- Use ElastiCache for frequently accessed data
- Implement database connection pooling
- Use Aurora Global Database for global reads
- Configure appropriate instance types
```

**Key Files:**
- `src/Stacks/CloudFrontStack.cs` - CDN implementation
- `src/Stacks/ElastiCacheStack.cs` - Caching layer
- `src/Stacks/AuroraStack.cs` - Database optimization

### 2.4 Serverless Architecture

**Exam Topics:**
- Lambda functions
- API Gateway
- DynamoDB
- EventBridge
- Step Functions

**Infrastructure Components:**
- Lambda for compute
- API Gateway for APIs
- DynamoDB for NoSQL
- EventBridge for event routing
- Step Functions for orchestration

**Practice Scenarios:**
```
Scenario 1: Design serverless API
- Use API Gateway for REST API
- Implement Lambda for business logic
- Store data in DynamoDB
- Use CloudWatch for monitoring

Scenario 2: Implement event-driven architecture
- Use EventBridge for event routing
- Trigger Lambda functions on events
- Orchestrate workflows with Step Functions
- Implement dead letter queues for failures
```

**Key Files:**
- `src/Stacks/ServerlessStack.cs` - Lambda functions
- `src/Stacks/ApiGatewayStack.cs` - API management
- `src/Stacks/DynamoDbStack.cs` - NoSQL database
- `src/Stacks/EventBridgeStack.cs` - Event routing
- `src/Stacks/StepFunctionsStack.cs` - Workflow orchestration

## Domain 3: Continuous Improvement (25%)

### 3.1 Monitoring and Observability

**Exam Topics:**
- CloudWatch metrics and logs
- X-Ray tracing
- CloudWatch Insights
- Custom metrics
- Alarms and notifications

**Infrastructure Components:**
- CloudWatch dashboards
- X-Ray instrumentation
- Container Insights
- Log aggregation
- Custom metrics

**Practice Scenarios:**
```
Scenario 1: Implement comprehensive monitoring
- Create CloudWatch dashboards for key metrics
- Configure alarms for critical thresholds
- Implement X-Ray for distributed tracing
- Set up log aggregation and analysis

Scenario 2: Troubleshoot performance issues
- Use X-Ray to identify bottlenecks
- Analyze CloudWatch Logs Insights
- Review Container Insights metrics
- Correlate metrics across services
```

**Key Files:**
- `src/Stacks/MonitoringStack.cs` - CloudWatch monitoring
- `src/Stacks/XRayStack.cs` - Distributed tracing
- `src/Stacks/ContainerInsightsStack.cs` - Container monitoring
- `src/Stacks/CloudWatchLogsStack.cs` - Log management

### 3.2 Disaster Recovery

**Exam Topics:**
- Backup strategies
- RTO and RPO requirements
- Pilot Light
- Warm Standby
- Multi-region failover

**Infrastructure Components:**
- AWS Backup
- Pilot Light architecture
- Warm Standby architecture
- Cross-region replication
- Aurora Global Database

**Practice Scenarios:**
```
Scenario 1: Design DR strategy with 1-hour RTO
- Implement Warm Standby in DR region
- Use Aurora Global Database
- Configure S3 cross-region replication
- Set up Route53 health checks for failover

Scenario 2: Implement cost-effective DR (4-hour RTO)
- Use Pilot Light architecture
- Replicate data to DR region
- Keep minimal infrastructure running
- Document scale-up procedures
```

**Key Files:**
- `src/Stacks/BackupStack.cs` - Backup strategy
- `src/Stacks/PilotLightStack.cs` - Pilot Light DR
- `src/Stacks/WarmStandbyStack.cs` - Warm Standby DR
- `src/Constructs/DisasterRecovery/` - DR constructs

### 3.3 Troubleshooting

**Exam Topics:**
- VPC Flow Logs
- CloudTrail logs
- CloudWatch Logs Insights
- X-Ray service map
- AWS Config

**Infrastructure Components:**
- VPC Flow Logs
- CloudTrail logging
- CloudWatch Logs
- X-Ray tracing
- AWS Config rules

**Practice Scenarios:**
```
Scenario 1: Troubleshoot network connectivity
- Review VPC Flow Logs for rejected traffic
- Check security group and NACL rules
- Verify route table configurations
- Test with VPC Reachability Analyzer

Scenario 2: Investigate security incident
- Review CloudTrail logs for API calls
- Check AWS Config for configuration changes
- Analyze VPC Flow Logs for traffic patterns
- Use GuardDuty findings
```

**Key Files:**
- `src/Stacks/VpcStack.cs` - VPC Flow Logs
- `src/Stacks/CloudTrailStack.cs` - API audit logs
- `src/Stacks/SecurityMonitoringStack.cs` - Security monitoring

## Domain 4: Migration and Modernization (20%)

### 4.1 Migration Strategies (7 Rs)

**Exam Topics:**
- Rehost (Lift and Shift)
- Replatform (Lift, Tinker, and Shift)
- Refactor/Re-architect
- Repurchase
- Retire
- Retain
- Relocate

**Infrastructure Components:**
- ECS for containerized workloads
- EKS for Kubernetes workloads
- Lambda for serverless refactoring
- Aurora for database migration

**Practice Scenarios:**
```
Scenario 1: Migrate monolithic application
- Phase 1: Rehost to EC2 (quick win)
- Phase 2: Replatform to containers (ECS)
- Phase 3: Refactor to microservices
- Phase 4: Adopt serverless where appropriate

Scenario 2: Database migration strategy
- Assess current database (Oracle/SQL Server)
- Use AWS DMS for data migration
- Migrate to Aurora PostgreSQL/MySQL
- Implement read replicas for cutover
```

**Key Files:**
- `src/Stacks/EcsStack.cs` - Container platform
- `src/Stacks/EksStack.cs` - Kubernetes platform
- `src/Stacks/ServerlessStack.cs` - Serverless refactoring

### 4.2 Container Orchestration

**Exam Topics:**
- ECS vs EKS
- Fargate vs EC2 launch types
- Service mesh (App Mesh)
- Container security
- Container monitoring

**Infrastructure Components:**
- ECS clusters
- EKS clusters
- App Mesh for service mesh
- Container Insights
- ECR for container registry

**Practice Scenarios:**
```
Scenario 1: Choose between ECS and EKS
- Use ECS for: AWS-native, simpler operations
- Use EKS for: Kubernetes expertise, portability
- Consider Fargate for: Serverless containers
- Use EC2 for: Cost optimization, control

Scenario 2: Implement service mesh
- Deploy App Mesh for microservices
- Configure virtual nodes and services
- Implement traffic routing policies
- Enable observability with X-Ray
```

**Key Files:**
- `src/Stacks/EcsStack.cs` - ECS implementation
- `src/Stacks/EksStack.cs` - EKS implementation
- `src/Stacks/AppMeshStack.cs` - Service mesh
- `src/Stacks/ContainerInsightsStack.cs` - Container monitoring

### 4.3 Modernization Patterns

**Exam Topics:**
- Microservices architecture
- Event-driven architecture
- Serverless patterns
- API-first design

**Infrastructure Components:**
- API Gateway for APIs
- EventBridge for events
- Lambda for functions
- Step Functions for workflows
- App Mesh for service mesh

**Practice Scenarios:**
```
Scenario 1: Decompose monolith to microservices
- Identify bounded contexts
- Extract services incrementally
- Use API Gateway for API management
- Implement event-driven communication

Scenario 2: Implement event-driven architecture
- Use EventBridge as event bus
- Decouple services with events
- Implement saga pattern with Step Functions
- Use DynamoDB Streams for change data capture
```

**Key Files:**
- `src/Stacks/ApiGatewayStack.cs` - API management
- `src/Stacks/EventBridgeStack.cs` - Event bus
- `src/Stacks/StepFunctionsStack.cs` - Orchestration
- `src/Stacks/AppMeshStack.cs` - Service mesh

## Exam Preparation Tips

### 1. Hands-On Practice
- Deploy this infrastructure in your AWS account
- Experiment with different configurations
- Break things and fix them
- Practice troubleshooting scenarios

### 2. Understand Trade-offs
- Cost vs Performance
- Availability vs Consistency
- Security vs Usability
- Complexity vs Maintainability

### 3. Know Service Limits
- VPC limits (5 per region default)
- EC2 instance limits
- RDS storage limits
- API Gateway throttling

### 4. Master Well-Architected Framework
- Operational Excellence
- Security
- Reliability
- Performance Efficiency
- Cost Optimization
- Sustainability

### 5. Practice Scenario-Based Questions
- Read the scenario carefully
- Identify requirements (RTO, RPO, cost, etc.)
- Eliminate obviously wrong answers
- Choose the most appropriate solution

## Study Resources

### AWS Documentation
- [AWS Well-Architected Framework](https://aws.amazon.com/architecture/well-architected/)
- [AWS Architecture Center](https://aws.amazon.com/architecture/)
- [AWS Whitepapers](https://aws.amazon.com/whitepapers/)

### Practice Exams
- AWS Official Practice Exam
- Tutorials Dojo Practice Tests
- Whizlabs Practice Tests

### Hands-On Labs
- AWS Workshops
- AWS Skill Builder
- This repository's infrastructure

## Exam Day Tips

1. **Time Management**
   - 180 minutes for 75 questions
   - ~2.4 minutes per question
   - Flag difficult questions and return later

2. **Question Strategy**
   - Read the entire question
   - Identify key requirements
   - Eliminate wrong answers
   - Choose the MOST appropriate solution

3. **Common Traps**
   - "Most cost-effective" vs "Most performant"
   - "Least operational overhead" vs "Most flexible"
   - Over-engineering vs Under-engineering

4. **Key Phrases**
   - "Most cost-effective" → Consider Spot, Reserved, Serverless
   - "Least operational overhead" → Managed services, Serverless
   - "Highly available" → Multi-AZ, Auto Scaling
   - "Disaster recovery" → Multi-region, Backups
   - "Real-time" → Kinesis, DynamoDB Streams
   - "Near real-time" → S3 events, EventBridge

## Practice Questions

### Question 1: Multi-Region Failover
**Scenario:** A company needs to implement a disaster recovery solution with an RTO of 1 hour and RPO of 15 minutes. The application uses Aurora database and serves global users.

**What is the MOST appropriate solution?**

A) Use Aurora Global Database with Warm Standby in DR region
B) Use Aurora snapshots copied to DR region with Pilot Light
C) Use Aurora Multi-AZ in single region
D) Use DMS for continuous replication to DR region

**Answer:** A
**Explanation:** Aurora Global Database provides <1 minute RPO and can meet 1-hour RTO with Warm Standby. Option B has higher RPO due to snapshot frequency. Option C doesn't provide DR. Option D is more complex than needed.

### Question 2: Cost Optimization
**Scenario:** A company runs batch processing jobs that are not time-sensitive and can tolerate interruptions. The jobs run for 2-4 hours daily.

**What is the MOST cost-effective compute option?**

A) On-Demand EC2 instances
B) Reserved Instances
C) Spot Instances
D) Fargate Spot

**Answer:** C or D (both acceptable)
**Explanation:** Spot Instances provide 70-90% savings and are perfect for interruptible workloads. Fargate Spot is also valid if containerized. Reserved Instances require commitment. On-Demand is most expensive.

### Question 3: Security Design
**Scenario:** A company needs to encrypt all data at rest and in transit. They require full control over encryption keys and need to rotate keys annually.

**What should they implement?**

A) AWS managed keys with automatic rotation
B) Customer managed KMS keys with manual rotation
C) Customer managed KMS keys with automatic rotation
D) CloudHSM for key management

**Answer:** C
**Explanation:** Customer managed KMS keys provide full control and support automatic annual rotation. AWS managed keys don't provide full control. Manual rotation is unnecessary. CloudHSM is overkill for this requirement.

## Next Steps

1. Deploy this infrastructure: [Deployment Guide](../deployment/README.md)
2. Review architecture: [Architecture Documentation](../architecture/README.md)
3. Understand costs: [Cost Estimation](../cost/README.md)
4. Practice scenarios: [Hands-On Labs](./hands-on-labs.md)
5. Take practice exams
6. Schedule your exam!

Good luck with your SAP-C02 certification! 🚀
