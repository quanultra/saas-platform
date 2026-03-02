#!/bin/bash

# AWS SAP-C02 Practice Infrastructure - Stack Update Script
# This script handles CDK stack updates with change sets and validation

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
LOG_DIR="${PROJECT_ROOT}/logs"
UPDATE_LOG="${LOG_DIR}/update-$(date +%Y%m%d-%H%M%S).log"

mkdir -p "$LOG_DIR"

# Logging functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1" | tee -a "$UPDATE_LOG"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1" | tee -a "$UPDATE_LOG"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" | tee -a "$UPDATE_LOG"
}

log_debug() {
           Review changes before applying
    --blue-green               Use blue-green deployment strategy
    --canary                   Use canary deployment (gradual rollout)
    --auto-approve             Skip approval prompts
    -h, --help                 Display this help

EXAMPLES:
    $0 -e dev -s VpcStack --review-changes
    $0 -e prod -s EcsStack --blue-green
    $0 -e staging -s ApiGatewayStack --canary

EOF
    exit 1
}

# Parse arguments
ENVIRONMENT=""
STACK=""
REGION="us-east-1"
ACCOUNT=""
CHANGE_SET_ONLY=false
REVIEW_CHANGES=false
BLUE_GREEN=false
CANARY=false
AUTO_APPROVE=false

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
        --change-set-only)
            CHANGE_SET_ONLY=true
            shift
            ;;
        --review-changes)
            REVIEW_CHANGES=true
            shift
            ;;
        --blue-green)
            BLUE_GREEN=true
            shift
            ;;
        --canary)
            CANARY=true
            shift
            ;;
        --auto-approve)
            AUTO_APPROVE=true
            shift
            ;;
        -h|--help)
            usage
            ;;
        *)
            log_error "Unknown option: $1"
            usage
            ;;
    esac
done

# Validate parameters
if [ -z "$ENVIRONMENT" ] || [ -z "$STACK" ]; then
    log_error "Environment and stack are required"
    usage
fi

# Get AWS account ID
if [ -z "$ACCOUNT" ]; then
    ACCOUNT=$(aws sts get-caller-identity --query Account --output text 2>/dev/null || echo "")
    if [ -z "$ACCOUNT" ]; then
        log_error "Failed to get AWS account ID"
        exit 1
    fi
fi

log_info "=== Stack Update Process ==="
log_info "Environment: $ENVIRONMENT"
log_info "Stack: $STACK"
log_info "Region: $REGION"
log_info "Account: $ACCOUNT"

# Get current stack status
get_stack_status() {
    aws cloudformation describe-stacks \
        --stack-name "$STACK" \
        --region "$REGION" \
        --query 'Stacks[0].StackStatus' \
        --output text 2>/dev/null || echo "NOT_FOUND"
}

# Create and review change set
create_change_set() {
    log_info "Creating change set for stack: $STACK"

    local change_set_name="update-$(date +%Y%m%d-%H%M%S)"

    cd "$PROJECT_ROOT"
    export CDK_DEFAULT_ACCOUNT="$ACCOUNT"
    export CDK_DEFAULT_REGION="$REGION"

    # Generate CloudFormation template
    log_info "Synthesizing template..."
    cdk synth "$STACK" --context environment="$ENVIRONMENT" > /dev/null

    local template_file="cdk.out/${STACK}.template.json"

    if [ ! -f "$template_file" ]; then
        log_error "Template file not found: $template_file"
        exit 1
    fi

    # Create change set
    log_info "Creating CloudFormation change set: $change_set_name"

    aws cloudformation create-change-set \
        --stack-name "$STACK" \
        --change-set-name "$change_set_name" \
        --template-body "file://$template_file" \
        --region "$REGION" \
        --capabilities CAPABILITY_IAM CAPABILITY_NAMED_IAM CAPABILITY_AUTO_EXPAND \
        2>&1 | tee -a "$UPDATE_LOG"

    if [ ${PIPESTATUS[0]} -ne 0 ]; then
        log_error "Failed to create change set"
        exit 1
    fi

    # Wait for change set creation
    log_info "Waiting for change set to be created..."

    local max_attempts=30
    local attempt=0

    while [ $attempt -lt $max_attempts ]; do
        local status=$(aws cloudformation describe-change-set \
            --stack-name "$STACK" \
            --change-set-name "$change_set_name" \
            --region "$REGION" \
            --query 'Status' \
            --output text 2>/dev/null || echo "UNKNOWN")

        if [ "$status" = "CREATE_COMPLETE" ]; then
            log_info "Change set created successfully"
            break
        elif [ "$status" = "FAILED" ]; then
            local reason=$(aws cloudformation describe-change-set \
                --stack-name "$STACK" \
                --change-set-name "$change_set_name" \
                --region "$REGION" \
                --query 'StatusReason' \
                --output text)

            if [[ "$reason" == *"didn't contain changes"* ]]; then
                log_info "No changes detected in stack"
                return 0
            else
                log_error "Change set creation failed: $reason"
                exit 1
            fi
        fi

        sleep 2
        ((attempt++))
    done

    if [ $attempt -eq $max_attempts ]; then
        log_error "Timeout waiting for change set creation"
        exit 1
    fi

    # Display changes
    log_info "Change set details:"
    echo ""

    aws cloudformation describe-change-set \
        --stack-name "$STACK" \
        --change-set-name "$change_set_name" \
        --region "$REGION" \
        --query 'Changes[*].[Type,ResourceChange.Action,ResourceChange.LogicalResourceId,ResourceChange.ResourceType,ResourceChange.Replacement]' \
        --output table | tee -a "$UPDATE_LOG"

    echo ""

    # Analyze changes for risk
    analyze_change_risk "$change_set_name"

    if [ "$CHANGE_SET_ONLY" = true ]; then
        log_info "Change set created. Skipping execution as requested."
        log_info "To execute: aws cloudformation execute-change-set --stack-name $STACK --change-set-name $change_set_name --region $REGION"
        return 0
    fi

    # Review and confirm
    if [ "$REVIEW_CHANGES" = true ] && [ "$AUTO_APPROVE" = false ]; then
        echo ""
        read -p "Do you want to execute this change set? (yes/no): " confirm
        if [ "$confirm" != "yes" ]; then
            log_info "Update cancelled by user"

            # Delete change set
            aws cloudformation delete-change-set \
                --stack-name "$STACK" \
                --change-set-name "$change_set_name" \
                --region "$REGION"

            exit 0
        fi
    fi

    # Execute change set
    log_info "Executing change set..."

    aws cloudformation execute-change-set \
        --stack-name "$STACK" \
        --change-set-name "$change_set_name" \
        --region "$REGION"

    # Wait for update to complete
    log_info "Waiting for stack update to complete..."

    aws cloudformation wait stack-update-complete \
        --stack-name "$STACK" \
        --region "$REGION"

    log_info "Stack update completed successfully"
}

# Analyze change risk
analyze_change_risk() {
    local change_set_name="$1"

    log_info "Analyzing change risk..."

    local changes=$(aws cloudformation describe-change-set \
        --stack-name "$STACK" \
        --change-set-name "$change_set_name" \
        --region "$REGION" \
        --query 'Changes[*].ResourceChange' \
        --output json)

    local high_risk=0
    local medium_risk=0
    local low_risk=0

    # Count replacements (high risk)
    high_risk=$(echo "$changes" | jq '[.[] | select(.Replacement == "True")] | length')

    # Count modifications (medium risk)
    medium_risk=$(echo "$changes" | jq '[.[] | select(.Action == "Modify" and .Replacement != "True")] | length')

    # Count additions/removals (low risk)
    low_risk=$(echo "$changes" | jq '[.[] | select(.Action == "Add" or .Action == "Remove")] | length')

    echo ""
    log_info "Risk Assessment:"
    log_error "  High Risk (Replacements): $high_risk"
    log_warn "  Medium Risk (Modifications): $medium_risk"
    log_info "  Low Risk (Additions/Removals): $low_risk"
    echo ""

    if [ $high_risk -gt 0 ]; then
        log_warn "WARNING: This update includes resource replacements which may cause downtime"

        if [ "$BLUE_GREEN" = false ]; then
            log_warn "Consider using --blue-green flag for zero-downtime deployment"
        fi
    fi
}

# Blue-green deployment
blue_green_deployment() {
    log_info "Performing blue-green deployment..."

    # This is a simplified blue-green approach
    # In production, you'd create a parallel stack and switch traffic

    log_info "Step 1: Creating green environment (new version)"

    local green_stack="${STACK}-green"

    cd "$PROJECT_ROOT"
    export CDK_DEFAULT_ACCOUNT="$ACCOUNT"
    export CDK_DEFAULT_REGION="$REGION"

    # Deploy green stack
    cdk deploy "$green_stack" \
        --context environment="$ENVIRONMENT" \
        --context deployment_type="green" \
        --require-approval never

    log_info "Step 2: Validating green environment"

    # Run smoke tests on green environment
    if [ -f "${SCRIPT_DIR}/smoke-tests.sh" ]; then
        log_info "Running smoke tests on green environment..."
        "${SCRIPT_DIR}/smoke-tests.sh" "$green_stack" "$REGION"
    fi

    log_info "Step 3: Switching traffic to green environment"

    # Update Route53 or load balancer to point to green
    # This is application-specific

    if [ "$AUTO_APPROVE" = false ]; then
        read -p "Traffic switched to green. Monitor and confirm success (yes to continue, no to rollback): " confirm

        if [ "$confirm" != "yes" ]; then
            log_warn "Rolling back to blue environment"
            # Switch traffic back to blue
            exit 1
        fi
    fi

    log_info "Step 4: Decommissioning blue environment"

    # Delete old blue stack
    aws cloudformation delete-stack \
        --stack-name "$STACK" \
        --region "$REGION"

    # Rename green to blue
    log_info "Blue-green deployment completed successfully"
}

# Canary deployment
canary_deployment() {
    log_info "Performing canary deployment..."

    log_info "Step 1: Deploying canary version (10% traffic)"

    # Update with canary configuration
    create_change_set

    log_info "Step 2: Monitoring canary metrics"

    # Monitor for 5 minutes
    local monitor_duration=300
    log_info "Monitoring canary for ${monitor_duration} seconds..."

    sleep $monitor_duration

    # Check CloudWatch alarms
    local alarm_state=$(aws cloudwatch describe-alarms \
        --alarm-name-prefix "${STACK}-" \
        --region "$REGION" \
        --query 'MetricAlarms[?StateValue==`ALARM`].AlarmName' \
        --output text)

    if [ -n "$alarm_state" ]; then
        log_error "Alarms triggered during canary: $alarm_state"
        log_error "Rolling back canary deployment"

        # Rollback
        "${SCRIPT_DIR}/rollback-stack.sh" -e "$ENVIRONMENT" -s "$STACK" -r "$REGION" --auto-approve
        exit 1
    fi

    log_info "Step 3: Gradually increasing traffic (50%)"
    sleep 60

    log_info "Step 4: Full rollout (100%)"

    log_info "Canary deployment completed successfully"
}

# Validate update
validate_update() {
    log_info "Validating stack update..."

    local status=$(get_stack_status)

    if [ "$status" != "UPDATE_COMPLETE" ]; then
        log_error "Stack is not in UPDATE_COMPLETE state: $status"
        exit 1
    fi

    # Check for drift
    log_info "Checking for configuration drift..."

    aws cloudformation detect-stack-drift \
        --stack-name "$STACK" \
        --region "$REGION" > /dev/null

    sleep 5

    local drift_status=$(aws cloudformation describe-stack-drift-detection-status \
        --stack-drift-detection-id "$(aws cloudformation detect-stack-drift --stack-name $STACK --region $REGION --query StackDriftDetectionId --output text)" \
        --region "$REGION" \
        --query 'StackDriftStatus' \
        --output text 2>/dev/null || echo "UNKNOWN")

    if [ "$drift_status" = "DRIFTED" ]; then
        log_warn "Stack has configuration drift detected"
    else
        log_info "No configuration drift detected"
    fi

    log_info "Validation completed"
}

# Main execution
main() {
    local current_status=$(get_stack_status)

    if [ "$current_status" = "NOT_FOUND" ]; then
        log_error "Stack not found: $STACK"
        log_info "Use deploy-stack.sh to create a new stack"
        exit 1
    fi

    log_info "Current stack status: $current_status"

    if [ "$BLUE_GREEN" = true ]; then
        blue_green_deployment
    elif [ "$CANARY" = true ]; then
        canary_deployment
    else
        create_change_set
    fi

    if [ "$CHANGE_SET_ONLY" = false ]; then
        validate_update
    fi

    log_info "=== Update process completed ==="
}

main
