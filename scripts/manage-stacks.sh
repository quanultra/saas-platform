#!/bin/bash

# AWS SAP-C02 Practice Infrastructure - Stack Management Helper
# This script provides a unified interface for all lifecycle operations

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Display banner
show_banner() {
    echo -e "${BLUE}"
    cat << "EOF"
╔═══════════════════════════════════════════════════════════╗
║   AWS SAP-C02 Practice Infrastructure                     ║
║   Stack Lifecycle Management                              ║
╚═══════════════════════════════════════════════════════════╝
EOF
    echo -e "${NC}"
}

# Display menu
show_menu() {
    echo ""
    echo -e "${GREEN}Available Operations:${NC}"
    echo ""
    echo "  1. Deploy stacks"
    echo "  2. Update stack"
    echo "  3. Delete stacks"
    echo "  4. Rollback stack"
    echo "  5. List stacks"
    echo "  6. Show stack status"
    echo "  7. Cleanup retained resources"
    echo "  8. View logs"
    echo "  9. Exit"
    echo ""
}

# List stacks
list_stacks() {
    echo -e "${GREEN}Listing st
Region [us-east-1]: " region
    region=${region:-us-east-1}

    echo ""
    echo -e "${BLUE}Stack Status:${NC}"

    aws cloudformation describe-stacks \
        --stack-name "$stack" \
        --region "$region" \
        --query 'Stacks[0].[StackName,StackStatus,CreationTime,LastUpdatedTime]' \
        --output table 2>/dev/null || echo "Stack not found"

    echo ""
    echo -e "${BLUE}Recent Events:${NC}"

    aws cloudformation describe-stack-events \
        --stack-name "$stack" \
        --region "$region" \
        --max-items 10 \
        --query 'StackEvents[*].[Timestamp,ResourceStatus,ResourceType,LogicalResourceId]' \
        --output table 2>/dev/null || echo "No events found"
}

# View logs
view_logs() {
    echo -e "${GREEN}Available log files:${NC}"
    echo ""

    local log_dir="$(dirname "$SCRIPT_DIR")/logs"

    if [ ! -d "$log_dir" ]; then
        echo "No logs directory found"
        return
    fi

    local logs=($(ls -t "$log_dir"/*.log 2>/dev/null))

    if [ ${#logs[@]} -eq 0 ]; then
        echo "No log files found"
        return
    fi

    for i in "${!logs[@]}"; do
        echo "  $((i+1)). $(basename "${logs[$i]}")"
    done

    echo ""
    read -p "Select log file (1-${#logs[@]}): " selection

    if [ "$selection" -ge 1 ] && [ "$selection" -le ${#logs[@]} ]; then
        local log_file="${logs[$((selection-1))]}"
        echo ""
        echo -e "${BLUE}Viewing: $(basename "$log_file")${NC}"
        echo ""
        less "$log_file"
    else
        echo "Invalid selection"
    fi
}

# Deploy operation
deploy_operation() {
    echo -e "${GREEN}Deploy Stacks${NC}"
    echo ""

    read -p "Environment (dev/staging/prod): " env
    read -p "Stack name (leave empty for all): " stack
    read -p "Region [us-east-1]: " region
    region=${region:-us-east-1}

    read -p "Dry run? (y/n) [n]: " dry_run
    read -p "Skip tests? (y/n) [n]: " skip_tests
    read -p "Auto approve? (y/n) [n]: " auto_approve
    read -p "Rollback on failure? (y/n) [y]: " rollback
    rollback=${rollback:-y}

    local cmd="${SCRIPT_DIR}/deploy-stack.sh -e $env -r $region"

    [ -n "$stack" ] && cmd="$cmd -s $stack"
    [ "$dry_run" = "y" ] && cmd="$cmd --dry-run"
    [ "$skip_tests" = "y" ] && cmd="$cmd --skip-tests"
    [ "$auto_approve" = "y" ] && cmd="$cmd --auto-approve"
    [ "$rollback" = "y" ] && cmd="$cmd --rollback-on-failure"

    echo ""
    echo -e "${BLUE}Executing: $cmd${NC}"
    echo ""

    eval "$cmd"
}

# Update operation
update_operation() {
    echo -e "${GREEN}Update Stack${NC}"
    echo ""

    read -p "Environment (dev/staging/prod): " env
    read -p "Stack name: " stack

    if [ -z "$stack" ]; then
        echo -e "${RED}Stack name is required for updates${NC}"
        return
    fi

    read -p "Region [us-east-1]: " region
    region=${region:-us-east-1}

    echo ""
    echo "Update strategy:"
    echo "  1. Standard update"
    echo "  2. Change set only (review)"
    echo "  3. Blue-green deployment"
    echo "  4. Canary deployment"
    read -p "Select strategy (1-4): " strategy

    local cmd="${SCRIPT_DIR}/update-stack.sh -e $env -s $stack -r $region"

    case $strategy in
        2)
            cmd="$cmd --change-set-only"
            ;;
        3)
            cmd="$cmd --blue-green"
            ;;
        4)
            cmd="$cmd --canary"
            ;;
    esac

    read -p "Review changes before applying? (y/n) [y]: " review
    review=${review:-y}
    [ "$review" = "y" ] && cmd="$cmd --review-changes"

    echo ""
    echo -e "${BLUE}Executing: $cmd${NC}"
    echo ""

    eval "$cmd"
}

# Delete operation
delete_operation() {
    echo -e "${GREEN}Delete Stacks${NC}"
    echo ""

    read -p "Environment (dev/staging/prod): " env
    read -p "Stack name (leave empty for all): " stack
    read -p "Region [us-east-1]: " region
    region=${region:-us-east-1}

    echo ""
    echo -e "${YELLOW}WARNING: This will delete infrastructure resources!${NC}"
    echo ""

    read -p "Retain data resources? (y/n) [y]: " retain_data
    retain_data=${retain_data:-y}

    read -p "Dry run first? (y/n) [y]: " dry_run
    dry_run=${dry_run:-y}

    local cmd="${SCRIPT_DIR}/delete-stack.sh -e $env -r $region"

    [ -n "$stack" ] && cmd="$cmd -s $stack"
    [ "$retain_data" = "y" ] && cmd="$cmd --retain-data"
    [ "$dry_run" = "y" ] && cmd="$cmd --dry-run"

    echo ""
    echo -e "${BLUE}Executing: $cmd${NC}"
    echo ""

    eval "$cmd"

    if [ "$dry_run" = "y" ]; then
        echo ""
        read -p "Proceed with actual deletion? (yes/no): " confirm
        if [ "$confirm" = "yes" ]; then
            cmd="${cmd/--dry-run/--force}"
            echo ""
            echo -e "${BLUE}Executing: $cmd${NC}"
            echo ""
            eval "$cmd"
        fi
    fi
}

# Rollback operation
rollback_operation() {
    echo -e "${GREEN}Rollback Stack${NC}"
    echo ""

    read -p "Environment (dev/staging/prod): " env
    read -p "Stack name: " stack

    if [ -z "$stack" ]; then
        echo -e "${RED}Stack name is required for rollback${NC}"
        return
    fi

    read -p "Region [us-east-1]: " region
    region=${region:-us-east-1}

    local cmd="${SCRIPT_DIR}/rollback-stack.sh -e $env -s $stack -r $region"

    echo ""
    echo -e "${BLUE}Executing: $cmd${NC}"
    echo ""

    eval "$cmd"
}

# Cleanup operation
cleanup_operation() {
    echo -e "${GREEN}Cleanup Retained Resources${NC}"
    echo ""

    read -p "Environment (dev/staging/prod): " env
    read -p "Region [us-east-1]: " region
    region=${region:-us-east-1}

    echo ""
    echo -e "${YELLOW}This will delete retained resources from previous deletions${NC}"
    echo ""

    read -p "Continue? (yes/no): " confirm

    if [ "$confirm" = "yes" ]; then
        local cmd="${SCRIPT_DIR}/delete-stack.sh -e $env -r $region --cleanup-retained"

        echo ""
        echo -e "${BLUE}Executing: $cmd${NC}"
        echo ""

        eval "$cmd"
    else
        echo "Cleanup cancelled"
    fi
}

# Main loop
main() {
    show_banner

    while true; do
        show_menu
        read -p "Select operation (1-9): " choice

        case $choice in
            1)
                deploy_operation
                ;;
            2)
                update_operation
                ;;
            3)
                delete_operation
                ;;
            4)
                rollback_operation
                ;;
            5)
                list_stacks
                ;;
            6)
                show_stack_status
                ;;
            7)
                cleanup_operation
                ;;
            8)
                view_logs
                ;;
            9)
                echo ""
                echo -e "${GREEN}Goodbye!${NC}"
                echo ""
                exit 0
                ;;
            *)
                echo -e "${RED}Invalid option${NC}"
                ;;
        esac

        echo ""
        read -p "Press Enter to continue..."
    done
}

# Run main
main
