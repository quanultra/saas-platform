# Monitoring và Observability Stacks

Tài liệu này mô tả các stack monitoring và observability đã được triển khai cho AWS SAP-C02 Practice Infrastructure.

## Tổng quan

Hệ thống monitoring bao gồm 4 stack chính:

1. **MonitoringStack** - CloudWatch Dashboards, Metrics và Alarms
2. **CloudWatchLogsStack** - Log Groups, Metric Filters và Log Insights Queries
3. **XRayStack** - Distributed Tracing với Sampling Rules và Service Maps
4. **ContainerInsightsStack** - Container monitoring cho ECS và EKS

## 1. MonitoringStack

### Chức năng
- Tạo custom CloudWatch dashboards
- Cấu hình metrics và alarms
- Thiết lập composite alarms
- SNS notifications cho alarms

### Dashboards

#### Main Dashboard
- EC2 CPU Utilization
- RDS Database Connections
- ECS Service CPU and Memory
- Lambda Invocations and Errors

#### Infrastructure Dashboard
- VPC Network Traffic
- Application Load Balancer metrics
- DynamoDB Operations
- S3 Requests

#### Application Dashboard
- API Gateway Requests và Errors
- Step Functions Executions
- Custom Application Metrics

### Alarms

#### Individual Alarms
- **HighCpuAlarm**: CPU > 80% trong 2 evaluation periods
- **HighMemoryAlarm**: Memory > 85% trong 2 evaluation periods
- **HighDiskUsageAlarm**: Disk usage > 90%
- **HighApiErrorRateAlarm**: API 5XX errors > 10 trong 5 phút

#### Composite Alarm
- **CriticalCompositeAlarm**: Kết hợp nhiều alarms với logic phức tạp
- Trigger khi có bất kỳ alarm nào ở trạng thái ALARM
- Hoặc khi cả disk và API error alarms cùng trigger

### Cấu hình

```csharp
var config = new StackConfiguration
{
    Monitoring = new MonitoringC
g Group
- Path: `/aws/{project}-{env}-infrastructure`
- Retention: Configurable (default 30 days)
- Purpose: Infrastructure logs

#### Security Log Group
- Path: `/aws/{project}-{env}-security`
- Retention: 1 year
- Purpose: Security events (always retained)

#### Audit Log Group
- Path: `/aws/{project}-{env}-audit`
- Retention: 1 year
- Purpose: Audit trails (always retained)

### Metric Filters

#### Application Metrics
- **ErrorCount**: Đếm số lượng errors
- **WarningCount**: Đếm số lượng warnings
- **ResponseTime**: Theo dõi response time
- **4XXErrors**: Client errors
- **5XXErrors**: Server errors

#### Infrastructure Metrics
- **DatabaseConnectionErrors**: Lỗi kết nối database
- **HighMemoryUsage**: Memory usage cao
- **DiskSpaceWarnings**: Cảnh báo disk space

#### Security Metrics
- **FailedAuthentications**: Đăng nhập thất bại
- **UnauthorizedAccess**: Truy cập không được phép
- **SuspiciousActivity**: Hoạt động đáng ngờ

### Log Insights Queries

#### Error Analysis Query
```
fields @timestamp, @message, level, error, stack
| filter level = "ERROR" or level = "FATAL"
| sort @timestamp desc
```

#### Performance Analysis Query
```
fields @timestamp, requestId, duration, statusCode, path
| filter duration > 1000
| sort duration desc
```

#### Security Events Query
```
fields @timestamp, eventType, sourceIP, userAgent, action, result
| filter eventType = "authentication" or eventType = "authorization"
| sort @timestamp desc
```

#### Top Errors Query
```
fields error
| filter level = "ERROR"
| stats count(*) as errorCount by error
| sort errorCount desc
| limit 10
```

### Cấu hình

```csharp
var config = new StackConfiguration
{
    Monitoring = new MonitoringConfiguration
    {
        LogRetentionDays = 30 // 1, 3, 5, 7, 14, 30, 60, 90, 120, 150, 180, 365
    }
};

var logsStack = new CloudWatchLogsStack(app, "CloudWatchLogsStack", props, config);
```

## 3. XRayStack

### Chức năng
- Cấu hình X-Ray sampling rules
- Tạo X-Ray groups cho trace analysis
- Thiết lập service maps
- IAM roles cho X-Ray

### Sampling Rules

#### Default Sampling Rule
- Priority: 1000
- Fixed Rate: 5%
- Reservoir Size: 1
- Áp dụng cho: Tất cả requests

#### High Priority Sampling Rule
- Priority: 100
- Fixed Rate: 50%
- Reservoir Size: 10
- Áp dụng cho: POST requests tới `/api/critical/*`

#### Debug Sampling Rule
- Priority: 1
- Fixed Rate: 100%
- Reservoir Size: 100
- Áp dụng cho: Requests tới `/api/debug/*`

#### Error Sampling Rule
- Priority: 10
- Fixed Rate: 100%
- Reservoir Size: 50
- Áp dụng cho: Requests có error = true

#### Slow Request Sampling Rule
- Priority: 20
- Fixed Rate: 100%
- Reservoir Size: 25
- Áp dụng cho: Requests có slow = true

### X-Ray Groups

#### Error Traces Group
- Filter: `error = true OR fault = true`
- Purpose: Tất cả traces có errors

#### Slow Traces Group
- Filter: `duration >= 3`
- Purpose: Traces chậm hơn 3 giây

#### Critical Path Group
- Filter: `service("api") { http.url CONTAINS "/api/critical" }`
- Purpose: Traces cho critical endpoints

#### Database Queries Group
- Filter: `annotation.database_type EXISTS`
- Purpose: Traces có database operations

#### External API Calls Group
- Filter: `http.url BEGINSWITH "https://api.external"`
- Purpose: Traces gọi external APIs

### Helper Methods

#### Enable X-Ray for Lambda
```csharp
var xrayStack = new XRayStack(app, "XRayStack", props, config);
xrayStack.EnableXRayForLambda(myLambdaFunction);
```

#### Get X-Ray Daemon Container for ECS
```csharp
var xrayStack = new XRayStack(app, "XRayStack", props, config);
var xrayContainer = xrayStack.GetXRayDaemonContainerDefinition();
```

## 4. ContainerInsightsStack

### Chức năng
- Enable Container Insights cho ECS và EKS
- Cấu hình performance monitoring
- Thiết lập log aggregation
- Tạo container-specific dashboards và alarms

### Log Groups

#### ECS Insights Log Group
- Path: `/aws/ecs/containerinsights/{cluster}/performance`
- Purpose: ECS performance metrics

#### EKS Insights Log Group
- Path: `/aws/eks/{cluster}/cluster`
- Purpose: EKS cluster metrics

#### Fluent Bit Log Group
- Path: `/aws/containerinsights/{cluster}/application`
- Purpose: Application logs từ containers

### IAM Roles

#### ECS Task Execution Role
- Managed Policies:
  - AmazonECSTaskExecutionRolePolicy
  - CloudWatchAgentServerPolicy
- Permissions: Logs, CloudWatch metrics, ECS describe

#### EKS Service Account Role
- Managed Policies:
  - CloudWatchAgentServerPolicy
- Permissions: Logs, CloudWatch metrics, EKS describe

### Container Dashboard

#### ECS Metrics
- Cluster CPU Utilization
- Cluster Memory Utilization
- Network Traffic (Rx/Tx bytes)
- Running Task Count

#### EKS Metrics
- Node CPU Utilization
- Node Memory Utilization
- Pod Count
- Network Traffic (Rx/Tx bytes)

### Alarms

#### ECS Alarms
- **EcsHighCpuAlarm**: CPU > 80%
- **EcsHighMemoryAlarm**: Memory > 85%

#### EKS Alarms
- **EksHighCpuAlarm**: Node CPU > 80%
- **EksHighMemoryAlarm**: Node Memory > 85%

### Helper Methods

#### Enable Container Insights for ECS
```csharp
var containerInsightsStack = new ContainerInsightsStack(app, "ContainerInsightsStack", props, config);
containerInsightsStack.EnableContainerInsightsForEcs(myEcsCluster);
```

#### Get Fluent Bit ConfigMap for EKS
```csharp
var containerInsightsStack = new ContainerInsightsStack(app, "ContainerInsightsStack", props, config);
var fluentBitConfig = containerInsightsStack.GetFluentBitConfigMap();
// Apply this ConfigMap to your EKS cluster
```

## Deployment

### Deploy tất cả monitoring stacks

```bash
# Deploy MonitoringStack
cdk deploy MonitoringStack

# Deploy CloudWatchLogsStack
cdk deploy CloudWatchLogsStack

# Deploy XRayStack
cdk deploy XRayStack

# Deploy ContainerInsightsStack
cdk deploy ContainerInsightsStack

# Hoặc deploy tất cả cùng lúc
cdk deploy MonitoringStack CloudWatchLogsStack XRayStack ContainerInsightsStack
```

### Configuration Example

```csharp
var config = new StackConfiguration
{
    Environment = "prod",
    ProjectName = "aws-sap-c02-practice",
    Monitoring = new MonitoringConfiguration
    {
        AlarmEmail = "ops-team@example.com",
        EnableXRay = true,
        EnableContainerInsights = true,
        LogRetentionDays = 30
    }
};

// Create all monitoring stacks
var monitoringStack = new MonitoringStack(app, "MonitoringStack", props, config);
var logsStack = new CloudWatchLogsStack(app, "CloudWatchLogsStack", props, config);
var xrayStack = new XRayStack(app, "XRayStack", props, config);
var containerInsightsStack = new ContainerInsightsStack(app, "ContainerInsightsStack", props, config);
```

## Best Practices

### 1. Log Retention
- Application logs: 30 days
- Security logs: 1 year (minimum)
- Audit logs: 1 year (minimum)
- Infrastructure logs: 30-90 days

### 2. Alarm Thresholds
- CPU: 80% (warning), 90% (critical)
- Memory: 85% (warning), 95% (critical)
- Disk: 90% (warning), 95% (critical)
- Error rate: Tùy thuộc vào application

### 3. X-Ray Sampling
- Default: 5% cho general traffic
- Critical paths: 50-100%
- Debug endpoints: 100%
- Errors: 100%

### 4. Container Insights
- Enable cho tất cả production clusters
- Monitor CPU, memory, network metrics
- Set up alarms cho resource utilization
- Use Fluent Bit cho log aggregation

## Troubleshooting

### Alarms không trigger
1. Kiểm tra metric có data không
2. Verify threshold values
3. Check evaluation periods
4. Ensure SNS topic có subscriptions

### Logs không xuất hiện
1. Verify IAM permissions
2. Check log group retention
3. Ensure application đang write logs đúng format
4. Check CloudWatch agent configuration

### X-Ray traces không hiển thị
1. Verify X-Ray daemon đang chạy
2. Check IAM permissions
3. Ensure sampling rules đúng
4. Verify application có X-Ray SDK

### Container Insights không có data
1. Verify Container Insights enabled
2. Check IAM roles
3. Ensure CloudWatch agent đang chạy
4. Check log groups có data không

## Cost Optimization

### 1. Log Retention
- Giảm retention period cho non-critical logs
- Use S3 archival cho long-term storage
- Enable log compression

### 2. Metrics
- Reduce metric resolution nếu không cần real-time
- Use metric filters thay vì custom metrics khi có thể
- Aggregate metrics ở application level

### 3. X-Ray
- Adjust sampling rates dựa trên traffic
- Use lower sampling cho non-critical paths
- Monitor X-Ray costs và adjust accordingly

### 4. Container Insights
- Enable chỉ cho production clusters
- Use appropriate log retention
- Monitor và optimize metric collection frequency

## References

- [CloudWatch Documentation](https://docs.aws.amazon.com/cloudwatch/)
- [X-Ray Documentation](https://docs.aws.amazon.com/xray/)
- [Container Insights Documentation](https://docs.aws.amazon.com/AmazonCloudWatch/latest/monitoring/ContainerInsights.html)
- [AWS SAP-C02 Exam Guide](https://aws.amazon.com/certification/certified-solutions-architect-professional/)
