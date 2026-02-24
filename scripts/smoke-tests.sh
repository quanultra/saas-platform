#!/bin/bash

# Smoke tests to verify basic functionality after deployment

echo "Running smoke tests..."

# Check if AWS CLI is available
if ! command -v aws &> /dev/null; then
    echo "❌ AWS CLI not found"
    exit 1
fi

# Test VPC creation
echo "Testing VPC resources..."
VPC_COUNT=$(aws ec2 describe-vpcs --filters "Name=tag:ManagedBy,Values=CDK" --query 'Vpcs | length(@)' --output text)
if [ "$VPC_COUNT" -gt 0 ]; then
    echo "✅ VPCs created successfully"
else
    echo "⚠️  No VPCs found"
fi

# Test S3 buckets
echo "Testing S3 buckets..."
BUCKET_COUNT=$(aws s3 ls | grep -c "sap-c02-practice" || true)
if [ "$BUCKET_COUNT" -gt 0 ]; then
    echo "✅ S3 buckets created successfully"
else
    echo "⚠️  No S3 buckets found"
fi

# Test RDS instances
echo "Testing RDS instances..."
RDS_COUNT=$(aws rds describe-db-instances --query 'DBInstances | length(@)' --output text)
if [ "$RDS_COUNT" -gt 0 ]; then
    echo "✅ RDS instances created successfully"
else
    echo "⚠️  No RDS instances found"
fi

# Test CloudFormation stacks
echo "Testing CloudFormation stacks..."
STACK_COUNT=$(aws cloudformation list-stacks --stack-status-filter CREATE_COMPLETE UPDATE_COMPLETE --query 'StackSummaries | length(@)' --output text)
if [ "$STACK_COUNT" -gt 0 ]; then
    echo "✅ CloudFormation stacks deployed successfully"
else
    echo "❌ No CloudFormation stacks found"
    exit 1
fi

echo ""
echo "✅ Smoke tests completed!"
