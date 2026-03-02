# Component Interaction Diagrams

## Network Flow Diagram

```mermaid
graph TB
    subgraph "Internet"
        User[End User]
    end

    subgraph "Edge Layer"
        R53[Route53]
        CF[CloudFront]
        WAF[WAF]
    end

    subgraph "Load Balancing"
        ALB[Application Load Balancer]
        APIGW[API Gateway]
    end

    subgraph "Compute Layer"
        ECS[ECS Cluster]
        EKS[EKS Cluster]
        Lambda[Lambda Functions]
        ASG[Auto Scaling Group]
    end

    subgraph "Integration Layer"
        EB[EventBridge]
        SF[Step Functions]
        AM[App Mesh]
    end

    subgraph "Data Layer"
        Aurora[Aurora Global]
        RDS[RDS]
        DDB[DynamoDB]
        EC[ElastiCache]
    end

    subgraph "Storage Layer"
        S3[S3 Buckets]
    end

    User --> R53
    R53 --> CF
    CF --> WAF
    WAF --> ALB
    WAF --> APIGW
    ALB --> ECS
    ALB --> EKS
    ALB --> ASG
    APIGW --> Lambda
    Lambda --> EB
    Lambda --> SF
    ECS --> AM
    EKS --> AM
    AM --> Aurora
    AM --> DDB
    Lambda --> Aurora
    Lambda --> DDB
    ECS --> R
_Aurora[Aurora Active]
        WS_S3[S3 Active]
        WS_Compute[Compute Scaled Down]
    end

    P_Aurora -->|Replication| DR_Aurora
    P_S3 -->|Cross-Region Replication| DR_S3
    P_Aurora -->|Replication| WS_Aurora
    P_S3 -->|Cross-Region Replication| WS_S3

    P_VPC -.->|Transit Gateway| DR_VPC
    P_VPC -.->|Transit Gateway| WS_VPC
```

## Monitoring & Observability Flow

```mermaid
graph TB
    subgraph "Application Layer"
        App[Applications]
        Container[Containers]
        Lambda[Lambda]
    end

    subgraph "Instrumentation"
        XRay[X-Ray SDK]
        CWAgent[CloudWatch Agent]
        CIAgent[Container Insights]
    end

    subgraph "Collection Layer"
        CWLogs[CloudWatch Logs]
        CWMetrics[CloudWatch Metrics]
        XRayService[X-Ray Service]
    end

    subgraph "Analysis Layer"
        CWInsights[CloudWatch Insights]
        CWDashboards[CloudWatch Dashboards]
        CWAlarms[CloudWatch Alarms]
    end

    subgraph "Action Layer"
        SNS[SNS Topics]
        Lambda2[Lambda Functions]
        ASG2[Auto Scaling]
    end

    App --> XRay
    Container --> CIAgent
    Lambda --> CWAgent

    XRay --> XRayService
    CWAgent --> CWLogs
    CWAgent --> CWMetrics
    CIAgent --> CWMetrics

    CWLogs --> CWInsights
    CWMetrics --> CWDashboards
    CWMetrics --> CWAlarms
    XRayService --> CWDashboards

    CWAlarms --> SNS
    CWAlarms --> Lambda2
    CWAlarms --> ASG2
```

## Security Architecture Flow

```mermaid
graph TB
    subgraph "Perimeter Security"
        WAF[WAF Rules]
        SG[Security Groups]
        NACL[Network ACLs]
    end

    subgraph "Identity & Access"
        IAM[IAM Roles/Policies]
        KMS[KMS Keys]
    end

    subgraph "Audit & Compliance"
        CT[CloudTrail]
        Config[AWS Config]
        SM[Security Monitoring]
    end

    subgraph "Data Protection"
        Encrypt[Encryption at Rest]
        TLS[TLS/SSL]
        Backup[AWS Backup]
    end

    subgraph "Threat Detection"
        GD[GuardDuty]
        Macie[Macie]
        Inspector[Inspector]
    end

    WAF --> CT
    SG --> CT
    IAM --> CT
    KMS --> CT

    CT --> SM
    Config --> SM

    KMS --> Encrypt
    KMS --> TLS

    SM --> GD
    SM --> Macie
```

## Data Flow - Write Path

```mermaid
sequenceDiagram
    participant Client
    participant ALB
    participant ECS
    participant Cache as ElastiCache
    participant DB as Aurora
    participant S3
    participant EventBridge

    Client->>ALB: HTTPS Request
    ALB->>ECS: Forward Request
    ECS->>Cache: Check Cache
    Cache-->>ECS: Cache Miss
    ECS->>DB: Write Data
    DB-->>ECS: Acknowledge
    ECS->>Cache: Update Cache
    ECS->>S3: Store Objects
    ECS->>EventBridge: Publish Event
    EventBridge->>Lambda: Trigger Processing
    ECS-->>ALB: Response
    ALB-->>Client: HTTPS Response
```

## Data Flow - Read Path

```mermaid
sequenceDiagram
    participant Client
    participant CloudFront
    participant ALB
    participant ECS
    participant Cache as ElastiCache
    participant DB as Aurora

    Client->>CloudFront: HTTPS Request
    CloudFront->>CloudFront: Check Edge Cache
    alt Cache Hit
        CloudFront-->>Client: Cached Response
    else Cache Miss
        CloudFront->>ALB: Forward Request
        ALB->>ECS: Route Request
        ECS->>Cache: Check Cache
        alt Cache Hit
            Cache-->>ECS: Return Data
        else Cache Miss
            ECS->>DB: Query Database
            DB-->>ECS: Return Data
            ECS->>Cache: Update Cache
        end
        ECS-->>ALB: Response
        ALB-->>CloudFront: Response
        CloudFront->>CloudFront: Cache Response
        CloudFront-->>Client: Response
    end
```

## Deployment Flow

```mermaid
graph LR
    subgraph "Development"
        Code[CDK Code]
        Synth[CDK Synth]
    end

    subgraph "CI/CD"
        Build[Build]
        Test[Test]
        Deploy[Deploy]
    end

    subgraph "Environments"
        Dev[Development]
        Staging[Staging]
        Prod[Production]
    end

    subgraph "Validation"
        Smoke[Smoke Tests]
        Integration[Integration Tests]
        Monitor[Monitoring]
    end

    Code --> Synth
    Synth --> Build
    Build --> Test
    Test --> Deploy
    Deploy --> Dev
    Dev --> Smoke
    Smoke --> Staging
    Staging --> Integration
    Integration --> Prod
    Prod --> Monitor
```

## Multi-Region Failover

```mermaid
sequenceDiagram
    participant R53 as Route53
    participant Primary as Primary Region
    participant Secondary as Secondary Region
    participant Monitor as Health Check

    Note over Primary: Normal Operation
    R53->>Primary: Route Traffic
    Primary-->>R53: Healthy

    Note over Primary: Failure Detected
    Monitor->>Primary: Health Check
    Primary--xMonitor: Unhealthy
    Monitor->>R53: Update Health Status

    Note over Secondary: Failover Initiated
    R53->>Secondary: Route Traffic
    Secondary->>Secondary: Scale Up (if Pilot Light)
    Secondary-->>R53: Healthy

    Note over Secondary: Serving Traffic
    R53->>Secondary: Continue Routing
```

## Event-Driven Architecture

```mermaid
graph TB
    subgraph "Event Sources"
        API[API Gateway]
        S3E[S3 Events]
        DDB[DynamoDB Streams]
        Custom[Custom Events]
    end

    subgraph "Event Bus"
        EB[EventBridge]
    end

    subgraph "Event Processing"
        Lambda1[Lambda - Validation]
        Lambda2[Lambda - Transform]
        SF[Step Functions]
    end

    subgraph "Event Targets"
        SQS[SQS Queue]
        SNS[SNS Topic]
        DB2[Database]
        S3T[S3 Storage]
    end

    API --> EB
    S3E --> EB
    DDB --> EB
    Custom --> EB

    EB --> Lambda1
    EB --> Lambda2
    EB --> SF

    Lambda1 --> SQS
    Lambda2 --> SNS
    SF --> DB2
    SF --> S3T
```
