#!/bin/bash

# AWS SAP-C02 Practice Infrastructure Deployment Script
# This script helps deploy the infrastructure to different environments

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check prerequisites
check_prerequisites() {
    print_info "Checking prerequisites..."

    # Check AWS CLI
    if ! command -v aws &> /dev/null; then
        print_error "AWS CLI is not installed. Please
edentials are not configured. Please run: aws configure"
        exit 1
    fi

    print_info "All prerequisites are met!"
}

# Function to display usage
usage() {
    echo "Usage: $0 [COMMAND] [OPTIONS]"
    echo ""
    echo "Commands:"
    echo "  synth       Synthesize CloudFormation templates"
    echo "  deploy      Deploy all stacks"
    echo "  destroy     Destroy all stacks"
    echo "  diff        Show differences between deployed and local stacks"
    echo "  bootstrap   Bootstrap CDK in AWS account"
    echo ""
    echo "Options:"
    echo "  -e, --environment ENV    Environment to deploy (dev, staging, prod). Default: dev"
    echo "  -s, --stack STACK        Deploy specific stack only"
    echo "  -r, --region REGION      AWS region. Default: us-east-1"
    echo "  -h, --help               Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 deploy -e dev"
    echo "  $0 deploy -e prod -s aws-sap-c02-practice-prod-vpc"
    echo "  $0 destroy -e dev"
    echo "  $0 bootstrap -r us-east-1"
}

# Parse command line arguments
COMMAND=""
ENVIRONMENT="dev"
STACK=""
REGION="us-east-1"

while [[ $# -gt 0 ]]; do
    case $1 in
        synth|deploy|destroy|diff|bootstrap)
            COMMAND=$1
            shift
            ;;
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -s|--stack)
            STACK="$2"
            shift 2
            ;;
        -r|--region)
            REGION="$2"
            shift 2
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

# Validate environment
if [[ ! "$ENVIRONMENT" =~ ^(dev|staging|prod)$ ]]; then
    print_error "Invalid environment: $ENVIRONMENT. Must be dev, staging, or prod."
    exit 1
fi

# Check prerequisites
check_prerequisites

# Get AWS account ID
AWS_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
print_info "AWS Account: $AWS_ACCOUNT"
print_info "Environment: $ENVIRONMENT"
print_info "Region: $REGION"

# Execute command
case $COMMAND in
    bootstrap)
        print_info "Bootstrapping CDK in account $AWS_ACCOUNT, region $REGION..."
        cdk bootstrap aws://$AWS_ACCOUNT/$REGION
        print_info "Bootstrap complete!"
        ;;

    synth)
        print_info "Synthesizing CloudFormation templates for $ENVIRONMENT environment..."
        cdk synth --context environment=$ENVIRONMENT
        print_info "Synthesis complete! Check cdk.out/ directory for templates."
        ;;

    deploy)
        print_info "Deploying to $ENVIRONMENT environment..."

        if [ -n "$STACK" ]; then
            print_info "Deploying stack: $STACK"
            cdk deploy $STACK --context environment=$ENVIRONMENT --require-approval never
        else
            print_warning "This will deploy ALL stacks. This may take 30-60 minutes."
            read -p "Continue? (y/n) " -n 1 -r
            echo
            if [[ $REPLY =~ ^[Yy]$ ]]; then
                cdk deploy --all --context environment=$ENVIRONMENT --require-approval never
            else
                print_info "Deployment cancelled."
                exit 0
            fi
        fi

        print_info "Deployment complete!"
        ;;

    destroy)
        print_warning "This will DESTROY all resources in $ENVIRONMENT environment!"
        print_warning "This action cannot be undone!"
        read -p "Are you sure? Type 'yes' to confirm: " -r
        echo
        if [[ $REPLY == "yes" ]]; then
            if [ -n "$STACK" ]; then
                print_info "Destroying stack: $STACK"
                cdk destroy $STACK --context environment=$ENVIRONMENT --force
            else
                print_info "Destroying all stacks..."
                cdk destroy --all --context environment=$ENVIRONMENT --force
            fi
            print_info "Destruction complete!"
        else
            print_info "Destruction cancelled."
            exit 0
        fi
        ;;

    diff)
        print_info "Showing differences for $ENVIRONMENT environment..."
        if [ -n "$STACK" ]; then
            cdk diff $STACK --context environment=$ENVIRONMENT
        else
            cdk diff --context environment=$ENVIRONMENT
        fi
        ;;

    *)
        print_error "No command specified."
        usage
        exit 1
        ;;
esac
