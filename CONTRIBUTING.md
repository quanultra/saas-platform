# Contributing to AWS SAP-C02 Practice Infrastructure

Cảm ơn bạn đã quan tâm đến việc đóng góp cho dự án! Tài liệu này cung cấp hướng dẫn về cách đóng góp hiệu quả.

## Table of Contents
- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Testing Requirements](#testing-requirements)

## Code of Conduct

Dự án này tuân thủ các nguyên tắc:
- Tôn trọng lẫn nhau
- Xây dựng môi trường học tập tích cực
- Chia sẻ kiến thức và kinh nghiệm
- Chấp nhận phản hồi mang tính xây dựng

## Getting Started

### Prerequisites
- .NET SDK 6.0+
- AWS CDK CLI
- AWS Account
- Git
- IDE (VS Code, Rider, hoặc Visual Studio)

### Setup Development Environment

```bash
# Clone repository
git clone https://github.com/[username]/aws-sap-c02-practice.git
cd aws-sap-c02-practice

# Copy environment template
cp .env.example .env
# Edit .env với AWS credentials của bạn

# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run tests
dotnet test

# Verify CDK setup
cdk synth
```

## Development Workflow

### 1. Create a Branch

```bash
# Checkout develop branch
git checkout develop
git pull origin develop

# Create feature branch
git checkout -b feature/your-feature-name

# Or for bug fixes
git checkout -b bugfix/issue-description
```

### 2. Make Changes

- Viết code theo coding standards
- Thêm unit tests cho code mới
- Thêm property-based tests nếu áp dụng
- Update documentation nếu cần

### 3. Test Your Changes

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=PropertyBased"

# Check code coverage
dotnet test --collect:"XPlat Code Coverage"

# Verify CDK synth
cdk synth

# Check for changes
cdk diff
```

### 4. Commit Changes

```bash
# Stage changes
git add .

# Commit with conventional commit message
git commit -m "feat(component): description of change"

# Push to remote
git push origin feature/your-feature-name
```

### 5. Create Pull Request

- Tạo PR từ feature branch vào develop
- Điền đầy đủ PR template
- Request review từ maintainers
- Đảm bảo CI checks pass

## Coding Standards

### C# Style Guide

```csharp
// Use PascalCase for classes, methods, properties
public class MultiRegionVpc : Construct
{
    public IVpc Vpc { get; }
    
    public MultiRegionVpc(Construct scope, string id, MultiRegionVpcProps props)
        : base(scope, id)
    {
        // Use camelCase for local variables
        var cidrBlock = props.CidrBlock;
        
        // Use meaningful names
        var applicationSecurityGroup = new SecurityGroup(this, "AppSG", ...);
    }
}

// Add XML documentation for public APIs
/// <summary>
/// Creates a multi-region VPC with cross-AZ redundancy.
/// </summary>
/// <param name="scope">The CDK scope</param>
/// <param name="id">The construct ID</param>
/// <param name="props">Configuration properties</param>
public MultiRegionVpc(Construct scope, string id, MultiRegionVpcProps props)
```

### CDK Best Practices

1. **Reusable Constructs**: Tạo constructs có thể tái sử dụng
2. **Props Pattern**: Sử dụng props objects cho configuration
3. **Tagging**: Luôn tag resources cho cost allocation
4. **Outputs**: Export important values qua CfnOutput
5. **Security**: Follow least privilege principle

### File Organization

```
src/
├── Constructs/          # Reusable CDK constructs
│   ├── Network/
│   ├── Security/
│   └── ...
├── Stacks/              # Stack definitions
├── Models/              # Data models
└── Utils/               # Utility classes

tests/
├── Unit/                # Unit tests
├── PropertyBased/       # Property-based tests
└── Integration/         # Integration tests
```

## Commit Guidelines

### Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code formatting
- `refactor`: Code refactoring
- `test`: Adding tests
- `chore`: Maintenance tasks

### Examples

```bash
feat(multi-region): add CloudFront distribution construct

Implement CloudFront distribution with:
- Multi-origin failover
- Custom cache policies
- Security headers

Closes #123

---

fix(security): correct IAM policy for S3 replication

The replication role was missing GetObjectVersion permission.

Fixes #456

---

docs(readme): update deployment instructions

Add section about CDK bootstrap requirements.
```

## Pull Request Process

### Before Submitting

- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex logic
- [ ] Documentation updated
- [ ] Tests added/updated
- [ ] All tests pass
- [ ] CDK synth successful
- [ ] No secrets committed

### PR Review Checklist

Reviewers will check:
- Code quality and readability
- Test coverage
- Security considerations
- Cost impact
- Documentation completeness
- CDK best practices

### Merging

- PRs require at least 1 approval
- All CI checks must pass
- Squash and merge to keep history clean
- Delete branch after merge

## Testing Requirements

### Unit Tests

```csharp
[Fact]
public void TestVpcCreation()
{
    var app = new App();
    var stack = new MultiRegionStack(app, "TestStack", props);
    var template = Template.FromStack(stack);
    
    template.ResourceCountIs("AWS::EC2::VPC", 1);
    template.HasResourceProperties("AWS::EC2::VPC", new Dictionary<string, object>
    {
        { "CidrBlock", "10.0.0.0/16" }
    });
}
```

### Property-Based Tests

```csharp
// Feature: aws-sap-c02-practice-infrastructure, Property 32: Cost Allocation Tags
[Fact]
public void AllResourcesHaveCostAllocationTags()
{
    Gen.String.Sample(env =>
    {
        var app = new App();
        var stack = new MultiRegionStack(app, "TestStack", new MultiRegionStackProps
        {
            Environment = env
        });
        
        var template = Template.FromStack(stack);
        var resources = template.FindResources("AWS::*");
        
        foreach (var resource in resources)
        {
            Assert.True(HasRequiredTags(resource.Value));
        }
    }, iter: 100);
}
```

### Test Coverage

- Minimum 80% code coverage
- All public APIs must have tests
- Critical paths must have property tests
- Integration tests for major workflows

## Questions?

- Open an issue for questions
- Join discussions in GitHub Discussions
- Check existing documentation in `/docs`

## License

By contributing, you agree that your contributions will be licensed under the project's license.
