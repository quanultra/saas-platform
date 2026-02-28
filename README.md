# AWS SAP-C02 Practice Infrastructure

Dự án thực hành toàn diện cho AWS Certified Solutions Architect - Professional (SAP-C02) sử dụng Infrastructure as Code với AWS CDK và C#.

## 📋 Tổng Quan

Dự án này cung cấp môi trường hands-on để thực hành 12 chủ đề chính của kỳ thi SAP-C02:

1. **Multi-Region Architecture** - Kiến trúc đa vùng với failover tự động
2. **Hybrid Cloud Connectivity** - VPN, Transit Gateway, Direct Connect
3. **Disaster Recovery** - Backup, Pilot Light, Warm Standby
4. **Security & Compliance** - WAF, KMS, GuardDuty, Security Hub
5. **High Availability** - Multi-AZ, Load Balancing, Auto Scaling
6. **Microservices** - ECS, EKS, App Mesh, Service Discovery
7. **Data Analytics** - Kinesis, Glue, Athena, Redshift
8. **Serverless** - Lambda, API Gateway, DynamoDB, Step Functions
9. **Cost Optimization** - Budgets, Tagging, Lifecycle Policies
10. **Migration** - DMS, DataSync, Migration Hub
11. **Monitoring** - CloudWatch, X-Ray, Container Insights
12. **Infrastructure as Code** - CDK Best Practices, Testing, CI/CD

## 🚀 Quick Start

### Prerequisites

- .NET SDK 8.0+
- AWS CDK CLI
- AWS Account với appropriate permissions
- Git

### Installation

```bash
# Clone repository
git clone https://github.com/[username]/aws-sap-c02-practice.git
cd aws-sap-c02-practice

# Copy environment template
cp .env.example .env
# Edit .env với AWS credentials

# Restore dependencies
dotnet restore

# Build project
dotnet build
```

### Bootstrap CDK

```bash
# Bootstrap primary region
cdk bootstrap aws://ACCOUNT-ID/us-east-1

# Bootstrap secondary region
cdk bootstrap aws://ACCOUNT-ID/eu-west-1
```

### Deploy

```bash
# Deploy all stacks to dev
cdk deploy --all --context environment=dev

# Deploy specific stack
cdk deploy MultiRegion-Primary --context environment=dev

# Destroy all stacks
cdk destroy --all
```

## 📁 Project Structure

```
aws-sap-c02-practice/
├── src/
│   ├── Constructs/          # Reusable CDK constructs
│   │   ├── DisasterRecovery/
│   │   ├── Storage/
│   │   ├── Network/
│   │   └── Database/
│   ├── Stacks/              # Stack definitions
│   ├── Models/              # Data models
│   └── Utils/               # Utility classes
├── tests/
│   ├── Unit/                # Unit tests
│   ├── PropertyTests/       # Property-based tests (FsCheck)
│   └── Integration/         # Integration tests
├── scripts/                 # Deployment and utility scripts
├── docs/                    # Documentation
└── .kiro/specs/            # Spec files
```

## 🧪 Testing

### Chạy Tất Cả Tests

```bash
# Run all tests (unit + property-based)
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run with detailed logging
dotnet test --logger "console;verbosity=detailed"
```

### Chạy Unit Tests

```bash
# Run all unit tests
dotnet test --filter "FullyQualifiedName~Unit"

# Run specific test class
dotnet test --filter "FullyQualifiedName~VpcStackTests"

# Run specific test method
dotnet test --filter "VpcStack_ShouldCreatePrimaryVpc"
```

### Chạy Property-Based Tests

```bash
# Run all property tests
dotnet test --filter "FullyQualifiedName~PropertyTests"

# Run disaster recovery property tests
dotnet test --filter "FullyQualifiedName~DisasterRecoveryPropertyTests"

# Run specific property test
dotnet test --filter "BackupRetentionCompliance"
dotnet test --filter "PilotLightRTO"
dotnet test --filter "DisasterRecoveryRPO"
```

### Code Coverage

```bash
# Generate code coverage report
dotnet test --collect:"XPlat Code Coverage"

# View coverage report (requires reportgenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

### List All Tests

```bash
# List all available tests
dotnet test --list-tests
```

## 💰 Cost Estimates

| Component | Monthly Cost |
|-----------|-------------|
| Multi-Region | $250-350 |
| Hybrid Cloud | $100-150 |
| Disaster Recovery | $100-300 |
| Security | $100-200 |
| High Availability | $300-500 |
| Microservices | $200-400 |
| Data Analytics | $300-600 |
| Serverless | $50-150 |
| Monitoring | $50-100 |

**Total**: $1,550 - $3,100/month

## 📊 Test Coverage

Dự án sử dụng property-based testing với FsCheck để đảm bảo tính đúng đắn của infrastructure:

### Property Tests cho Disaster Recovery

- **Property 4: Backup retention compliance** - Validates backup plans meet minimum retention requirements (7+ days)
- **Property 5: RTO < 1 giờ cho Pilot Light** - Validates Recovery Time Objective under 1 hour
- **Property 6: RPO < 15 phút** - Validates Recovery Point Objective under 15 minutes through continuous backup

### Unit Tests

- VPC Stack Tests (12 tests)
- CloudFront Stack Tests (11 tests)
- RDS Stack Tests (4 tests)
- S3 Stack Tests (8 tests)
- Route53 Stack Tests (8 tests)
- VPN Stack Tests (7 tests)
- Transit Gateway Stack Tests (7 tests)

## 📚 Documentation

- [Requirements](.kiro/specs/aws-sap-c02-practice-infrastructure/requirements.md)
- [Design](.kiro/specs/aws-sap-c02-practice-infrastructure/design.md)
- [Tasks](.kiro/specs/aws-sap-c02-practice-infrastructure/tasks.md)
- [Contributing](CONTRIBUTING.md)

## 🤝 Contributing

Xem [CONTRIBUTING.md](CONTRIBUTING.md) để biết chi tiết về quy trình đóng góp.

## 📝 License

This project is for educational purposes.

## 🔗 Resources

- [AWS SAP-C02 Exam Guide](https://aws.amazon.com/certification/certified-solutions-architect-professional/)
- [AWS CDK Documentation](https://docs.aws.amazon.com/cdk/)
- [AWS Well-Architected Framework](https://aws.amazon.com/architecture/well-architected/)
- [FsCheck Documentation](https://fscheck.github.io/FsCheck/)
