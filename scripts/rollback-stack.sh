#!/bin/bash

# AWS SAP-C02 Practice Infrastructure - Stack Rollback Script
# This script handles rollback of failed deployments

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
LOG_DIR="${PROJECT_ROOT}/logs"
ROLLBACK_LOG="${LOG_DIR}/rollback-$(date +%Y%m%d-%H%M%S).log"

mkdir -p "$LOG_DIR"

# Logging functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1" | tee -a "$ROLLBACK_LOG"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1" | tee -a "$ROLLBACK_LOG"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" | tee -a "$ROLLBACK_LOG"
 VpcStack --to-version 5

EOF
    exit 1
}

# Parse arguments
ENVIRONMENT=""
STACK=""
REGION="us-east-1"
TO_VERSION=""
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
        --to-version)
            TO_VERSION="$2"
            shift 2
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

log_info "=== Stack Rollback Process ==="
log_info "Environment: $ENVIRONMENT"
log_info "Stack: $STACK"
log_info "Region: $REGION"

# Get current stack status
get_stack_status() {
    aws cloudformation describe-stacks \
        --stack-name "$STACK" \
        --region "$REGION" \
        --query 'Stacks[0].StackStatus' \
        --output text 2>/dev/null || echo "NOT_FOUND"
}

# Get stack events
get_recent_events() {
    log_info "Recent stack events:"
    aws cloudformation describe-stack-events \
        --stack-name "$STACK" \
        --region "$REGION" \
        --max-items 10 \
        --query 'StackEvents[*].[Timestamp,ResourceStatus,ResourceType,LogicalResourceId,ResourceStatusReason]' \
        --output table
}

# Perform rollback
perform_rollback() {
    local current_status=$(get_stack_status)

    log_info "Current stack status: $current_status"

    case "$current_status" in
        "UPDATE_ROLLBACK_FAILED"|"UPDATE_FAILED")
            log_info "Stack is in failed state. Initiating rollback..."

            if [ "$AUTO_APPROVE" = false ]; then
                read -p "Continue with rollback? (yes/no): " confirm
                if [ "$confirm" != "yes" ]; then
                    log_info "Rollback cancelled by user"
                    exit 0
                fi
            fi

            aws cloudformation continue-update-rollback \
                --stack-name "$STACK" \
                --region "$REGION"

            log_info "Rollback initiated. Waiting for completion..."

            aws cloudformation wait stack-rollback-complete \
                --stack-name "$STACK" \
                --region "$REGION"

            log_info "Rollback completed successfully"
            ;;

        "CREATE_FAILED"|"ROLLBACK_COMPLETE")
            log_warn "Stack creation failed. Consider deleting the stack."
            log_info "To delete: aws cloudformation delete-stack --stack-name $STACK --region $REGION"
            ;;

        "UPDATE_COMPLETE"|"CREATE_COMPLETE")
            log_warn "Stack is in complete state. No rollback needed."

            if [ -n "$TO_VERSION" ]; then
                log_info "Rolling back to version $TO_VERSION..."
                # This would require storing previous templates
                log_error "Version-based rollback not yet implemented"
                exit 1
            fi
            ;;

        "NOT_FOUND")
            log_error "Stack not found: $STACK"
            exit 1
            ;;

        *)
            log_error "Stack is in state: $current_status"
            log_error "Cannot perform rollback in this state"
            exit 1
            ;;
    esac
}

# Main execution
main() {
    log_info "Checking stack status..."
    get_recent_events
    perform_rollback

    log_info "=== Rollback process completed ==="
    log_info "Final status: $(get_stack_status)"
}

main
