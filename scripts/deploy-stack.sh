#!/bin/bash

# AWS SAP-C02 Practice Infrastructure - Stack Deployment Script
# This script handles CDK stack deployment with error handling, dependencies, and rollback

set -e  # Exit on error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
LOG_DIR="${PROJECT_ROOT}/logs"
DEPLOYMENT_LOG="${LOG_DIR}/deployment-$(date +%Y%m%d-%H%M%S).log"

# Create log directory if it doesn't exist
mkdir -p "$LOG_DIR"

# Logging functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1" | tee -a "$DEPLOYMENT_LOG"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1" | tee -a "$DEPLOYMENT_LOG"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" | tee -a "$DEPLOYMENT_LOG"
}

# Usage information
usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Deploy AWS CDK stacks with error handling and rollback support.

OPTIONS:
    -e, --environment ENV       Environment to deploy (dev, staging, prod)
    -s, --stack STACK          Specific stack to deploy (optional, deploys all if not specified)
    -r, --region REGION        AWS region (default: us-east-1)
    -a, --account ACCOUNT      AWS account ID
    -d, --dry-run              Perform a dry run (synth only)
    --skip-tests               Skip running tests before deployment
    --auto-approve             Skip approval prompts
    --rollback-on-failure      Automatically rollback on deployment failure
    -h, --help                 Display this help message

EXAMPLES:
    $0 -e dev -r us-east-1
    $0 -e prod -s VpcStack -a 123456789012
    $0 -e staging --dry-run

EOF
    exit 1
}

# Parse command line arguments
ENVIRONMENT=""
STACK=""
REGION="us-east-1"
ACCOUNT=""
DRY_RUN=false
SKIP_TESTS=false
AUTO_APPROVE=false
ROLLBACK_ON_FAILURE=false

while [[ $# -gt 0 ]]; do
    case $1 in
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
        -a|--account)
            ACCOUNT="$2"
            shift 2
            ;;
        -d|--dry-run)
            DRY_RUN=true
            shift
            ;;
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --auto-approve)
ts get-caller-identity --query Account --output text 2>/dev/null || echo "")
    if [ -z "$ACCOUNT" ]; then
        log_error "Failed to get AWS account ID. Please provide it with -a option or configure AWS CLI"
        exit 1
    fi
fi

log_info "Starting deployment process..."
log_info "Environment: $ENVIRONMENT"
log_info "Region: $REGION"
log_info "Account: $ACCOUNT"
log_info "Stack: ${STACK:-all}"

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."

    # Check AWS CLI
    if ! command -v aws &> /dev/null; then
        log_error "AWS CLI is not installed"
        exit 1
    fi

    # Check CDK CLI
    if ! command -v cdk &> /dev/null; then
        log_error "AWS CDK CLI is not installed. Run: npm install -g aws-cdk"
        exit 1
    fi

    # Check .NET SDK
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK is not installed"
        exit 1
    fi

    # Verify AWS credentials
    if ! aws sts get-caller-identity &> /dev/null; then
        log_error "AWS credentials are not configured or invalid"
        exit 1
    fi

    log_info "All prerequisites met"
}

# Build the project
build_project() {
    log_info "Building project..."

    cd "$PROJECT_ROOT"

    if ! dotnet restore; then
        log_error "Failed to restore dependencies"
        exit 1
    fi

    if ! dotnet build --configuration Release; then
        log_error "Failed to build project"
        exit 1
    fi

    log_info "Build completed successfully"
}

# Run tests
run_tests() {
    if [ "$SKIP_TESTS" = true ]; then
        log_warn "Skipping tests as requested"
        return 0
    fi

    log_info "Running tests..."

    cd "$PROJECT_ROOT"

    if ! dotnet test --configuration Release --no-build; then
        log_error "Tests failed"
        exit 1
    fi

    log_info "All tests passed"
}

# Bootstrap CDK if needed
bootstrap_cdk() {
    log_info "Checking CDK bootstrap status..."

    # Check if bootstrap stack exists
    if aws cloudformation describe-stacks --stack-name CDKToolkit --region "$REGION" &> /dev/null; then
        log_info "CDK already bootstrapped in $REGION"
    else
        log_info "Bootstrapping CDK in $REGION..."
        if ! cdk bootstrap "aws://${ACCOUNT}/${REGION}"; then
            log_error "Failed to bootstrap CDK"
            exit 1
        fi
        log_info "CDK bootstrap completed"
    fi
}

# Synthesize CloudFormation templates
synthesize_stacks() {
    log_info "Synthesizing CloudFormation templates..."

    cd "$PROJECT_ROOT"

    export CDK_DEFAULT_ACCOUNT="$ACCOUNT"
    export CDK_DEFAULT_REGION="$REGION"

    if [ -n "$STACK" ]; then
        if ! cdk synth "$STACK" --context environment="$ENVIRONMENT"; then
            log_error "Failed to synthesize stack: $STACK"
            exit 1
        fi
    else
        if ! cdk synth --context environment="$ENVIRONMENT"; then
            log_error "Failed to synthesize stacks"
            exit 1
        fi
    fi

    log_info "Synthesis completed successfully"
}

# Deploy stacks
deploy_stacks() {
    log_info "Deploying stacks..."

    cd "$PROJECT_ROOT"

    export CDK_DEFAULT_ACCOUNT="$ACCOUNT"
    export CDK_DEFAULT_REGION="$REGION"

    local deploy_cmd="cdk deploy"

    if [ -n "$STACK" ]; then
        deploy_cmd="$deploy_cmd $STACK"
    else
        deploy_cmd="$deploy_cmd --all"
    fi

    deploy_cmd="$deploy_cmd --context environment=$ENVIRONMENT"

    if [ "$AUTO_APPROVE" = true ]; then
        deploy_cmd="$deploy_cmd --require-approval never"
    fi

    # Add rollback configuration
    if [ "$ROLLBACK_ON_FAILURE" = true ]; then
        deploy_cmd="$deploy_cmd --rollback true"
    else
        deploy_cmd="$deploy_cmd --rollback false"
    fi

    log_info "Executing: $deploy_cmd"

    if ! eval "$deploy_cmd"; then
        log_error "Deployment failed"

        if [ "$ROLLBACK_ON_FAILURE" = true ]; then
            log_warn "Rollback was enabled, CloudFormation will automatically rollback changes"
        fi

        exit 1
    fi

    log_info "Deployment completed successfully"
}

# Verify deployment
verify_deployment() {
    log_info "Verifying deployment..."

    # Get list of stacks to verify
    local stacks_to_verify
    if [ -n "$STACK" ]; then
        stacks_to_verify="$STACK"
    else
        # Get all stacks from cdk.json or list command
        stacks_to_verify=$(cdk list --context environment="$ENVIRONMENT" 2>/dev/null || echo "")
    fi

    if [ -z "$stacks_to_verify" ]; then
        log_warn "No stacks found to verify"
        return 0
    fi

    local failed_stacks=()

    for stack in $stacks_to_verify; do
        log_info "Verifying stack: $stack"

        local stack_status=$(aws cloudformation describe-stacks \
            --stack-name "$stack" \
            --region "$REGION" \
            --query 'Stacks[0].StackStatus' \
            --output text 2>/dev/null || echo "NOT_FOUND")

        if [[ "$stack_status" == "CREATE_COMPLETE" ]] || [[ "$stack_status" == "UPDATE_COMPLETE" ]]; then
            log_info "Stack $stack is in healthy state: $stack_status"
        else
            log_error "Stack $stack is in unexpected state: $stack_status"
            failed_stacks+=("$stack")
        fi
    done

    if [ ${#failed_stacks[@]} -gt 0 ]; then
        log_error "The following stacks failed verification: ${failed_stacks[*]}"
        exit 1
    fi

    log_info "All stacks verified successfully"
}

# Cleanup on error
cleanup_on_error() {
    log_error "Deployment failed. Cleaning up..."

    if [ "$ROLLBACK_ON_FAILURE" = true ]; then
        log_info "Automatic rollback is enabled, CloudFormation will handle cleanup"
    else
        log_warn "Manual cleanup may be required. Check CloudFormation console for stack status"
        log_warn "To manually rollback, run: ./scripts/rollback-stack.sh -e $ENVIRONMENT -s $STACK"
    fi
}

# Main execution
main() {
    # Set up error trap
    trap cleanup_on_error ERR

    log_info "=== AWS CDK Stack Deployment ==="
    log_info "Log file: $DEPLOYMENT_LOG"

    check_prerequisites
    build_project
    run_tests
    bootstrap_cdk
    synthesize_stacks

    if [ "$DRY_RUN" = true ]; then
        log_info "Dry run completed. Skipping actual deployment."
        log_info "Review synthesized templates in cdk.out/ directory"
        exit 0
    fi

    deploy_stacks
    verify_deployment

    log_info "=== Deployment completed successfully ==="
    log_info "Environment: $ENVIRONMENT"
    log_info "Region: $REGION"
    log_info "Timestamp: $(date)"
}

# Run main function
main
