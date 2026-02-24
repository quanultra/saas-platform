#!/bin/bash

# Script to setup Git hooks for the project

echo "Setting up Git hooks..."

# Create hooks directory if it doesn't exist
mkdir -p .git/hooks

# Pre-commit hook
cat > .git/hooks/pre-commit << 'EOF'
#!/bin/bash

echo "Running pre-commit checks..."

# Check for secrets
echo "Checking for secrets..."
if git diff --cached --name-only | xargs grep -i "aws_secret_access_key\|password\|api_key" 2>/dev/null; then
    echo "❌ Error: Potential secrets detected in staged files!"
    echo "Please remove secrets before committing."
    exit 1
fi

# Check code formatting
echo "Checking code formatting..."
dotnet format --verify-no-changes --verbosity quiet
if [ $? -ne 0 ]; then
    echo "❌ Error: Code formatting issues detected!"
    echo "Run 'dotnet format' to fix formatting."
    exit 1
fi

# Run tests
echo "Running tests..."
dotnet test --no-build --verbosity quiet
if [ $? -ne 0 ]; then
    echo "❌ Error: Tests failed!"
    exit 1
fi

# CDK synth check
echo "Checking CDK synth..."
cdk synth > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "❌ Error: CDK synth failed!"
    exit 1
fi

echo "✅ Pre-commit checks passed!"
exit 0
EOF

# Make pre-commit hook executable
chmod +x .git/hooks/pre-commit

# Commit-msg hook
cat > .git/hooks/commit-msg << 'EOF'
#!/bin/bash

# Validate commit message format
commit_msg_file=$1
commit_msg=$(cat "$commit_msg_file")

# Check conventional commit format
if ! echo "$commit_msg" | grep -qE "^(feat|fix|docs|style|refactor|test|chore)(\(.+\))?: .+"; then
    echo "❌ Error: Commit message does not follow conventional commits format!"
    echo ""
    echo "Format: <type>(<scope>): <subject>"
    echo ""
    echo "Types: feat, fix, docs, style, refactor, test, chore"
    echo ""
    echo "Example: feat(multi-region): add CloudFront distribution"
    exit 1
fi

echo "✅ Commit message format is valid"
exit 0
EOF

# Make commit-msg hook executable
chmod +x .git/hooks/commit-msg

echo "✅ Git hooks setup complete!"
echo ""
echo "Hooks installed:"
echo "  - pre-commit: Checks formatting, tests, and CDK synth"
echo "  - commit-msg: Validates conventional commit format"
