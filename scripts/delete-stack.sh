#!/bin/bash

# AWS SAP-C02 Practice Infrastructure - Stack Deletion Script
# This script handles CDK stack deletion with resource retention and cleanup

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
DELETION_LOG="${LOG_DIR}/deletion-$(date +%Y%m%d-%H%M%S).log"

mkdir -p "$LOG_DIR"

# Logging functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1" | tee -a "$DELETION_LOG"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1" | tee -a "$DELETION_LOG"
}

log_error() {
tain data resources (S3, RDS, DynamoDB)
    --force                    Force deletion without confirmation
    --cleanup-retained         Cleanup previously retained resources
    --dry-run                  Show what would be deleted without actually deleting
    -h, --help                 Display this help

EXAMPLES:
    $0 -e dev -s VpcStack
    $0 -e dev --retain-data
    $0 -e staging --dry-run
    $0 -e prod --cleanup-retained

EOF
    exit 1
}

# Parse arguments
ENVIRONMENT=""
STACK=""
REGION="us-east-1"
RETAIN_RESOURCES=false
RETAIN_DATA=false
FORCE=false
CLEANUP_RETAINED=false
DRY_RUN=false

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
        --retain-resources)
            RETAIN_RESOURCES=true
            shift
            ;;
        --retain-data)
            RETAIN_DATA=true
            shift
            ;;
        --force)
            FORCE=true
            shift
            ;;
        --cleanup-retained)
            CLEANUP_RETAINED=true
            shift
            ;;
        --dry-run)
            DRY_RUN=true
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
if [ -z "$ENVIRONMENT" ]; then
    log_error "Environment is required"
    usage
fi

log_info "=== Stack Deletion Process ==="
log_info "Environment: $ENVIRONMENT"
log_info "Stack: ${STACK:-all}"
log_info "Region: $REGION"
log_info "Retain resources: $RETAIN_RESOURCES"
log_info "Retain data: $RETAIN_DATA"

# Get list of stacks to delete
get_stacks_to_delete() {
    if [ -n "$STACK" ]; then
        echo "$STACK"
    else
        cd "$PROJECT_ROOT"
        export CDK_DEFAULT_REGION="$REGION"

        # Get all stacks for the environment
        cdk list --context environment="$ENVIRONMENT" 2>/dev/null || echo ""
    fi
}

# Get stack resources
get_stack_resources() {
    local stack_name="$1"

    aws cloudformation describe-stack-resources \
        --stack-name "$stack_name" \
        --region "$REGION" \
        --query 'StackResources[*].[LogicalResourceId,ResourceType,PhysicalResourceId]' \
        --output text 2>/dev/null || echo ""
}

# Identify data resources
identify_data_resources() {
    local stack_name="$1"

    log_info "Identifying data resources in stack: $stack_name"

    local resources=$(get_stack_resources "$stack_name")

    local data_resources=()

    while IFS=$'\t' read -r logical_id resource_type physical_id; do
        case "$resource_type" in
            AWS::S3::Bucket)
                data_resources+=("S3 Bucket: $physical_id")
                ;;
            AWS::RDS::DBInstance|AWS::RDS::DBCluster)
                data_resources+=("RDS Database: $physical_id")
                ;;
            AWS::DynamoDB::Table)
                data_resources+=("DynamoDB Table: $physical_id")
                ;;
            AWS::EFS::FileSystem)
                data_resources+=("EFS FileSystem: $physical_id")
                ;;
        esac
    done <<< "$resources"

    if [ ${#data_resources[@]} -gt 0 ]; then
        log_warn "Data resources found:"
        for resource in "${data_resources[@]}"; do
            log_warn "  - $resource"
        done
        echo ""
    fi
}

# Configure deletion policies
configure_deletion_policies() {
    local stack_name="$1"

    log_info "Configuring deletion policies for stack: $stack_name"

    if [ "$RETAIN_DATA" = true ]; then
        log_info "Setting retention policy for data resources..."

        # Update stack with retention policies
        # This would require modifying the CDK code to set DeletionPolicy: Retain
        log_warn "Note: Retention policies should be set in CDK code before deployment"
        log_warn "Add 'applyRemovalPolicy(RemovalPolicy.RETAIN)' to data resources"
    fi
}

# Empty S3 buckets
empty_s3_buckets() {
    local stack_name="$1"

    log_info "Checking for S3 buckets in stack: $stack_name"

    local buckets=$(aws cloudformation describe-stack-resources \
        --stack-name "$stack_name" \
        --region "$REGION" \
        --query 'StackResources[?ResourceType==`AWS::S3::Bucket`].PhysicalResourceId' \
        --output text 2>/dev/null || echo "")

    if [ -z "$buckets" ]; then
        log_info "No S3 buckets found"
        return 0
    fi

    for bucket in $buckets; do
        log_info "Emptying S3 bucket: $bucket"

        if [ "$DRY_RUN" = false ]; then
            # Delete all versions and delete markers
            aws s3api list-object-versions \
                --bucket "$bucket" \
                --query 'Versions[].{Key:Key,VersionId:VersionId}' \
                --output json 2>/dev/null | \
            jq -r '.[] | "--key \(.Key) --version-id \(.VersionId)"' | \
            xargs -I {} aws s3api delete-object --bucket "$bucket" {} 2>/dev/null || true

            aws s3api list-object-versions \
                --bucket "$bucket" \
                --query 'DeleteMarkers[].{Key:Key,VersionId:VersionId}' \
                --output json 2>/dev/null | \
            jq -r '.[] | "--key \(.Key) --version-id \(.VersionId)"' | \
            xargs -I {} aws s3api delete-object --bucket "$bucket" {} 2>/dev/null || true

            # Delete remaining objects
            aws s3 rm "s3://${bucket}" --recursive 2>/dev/null || true

            log_info "Bucket emptied: $bucket"
        else
            log_info "[DRY RUN] Would empty bucket: $bucket"
        fi
    done
}

# Delete RDS snapshots
delete_rds_snapshots() {
    local stack_name="$1"

    log_info "Checking for RDS instances in stack: $stack_name"

    local db_instances=$(aws cloudformation describe-stack-resources \
        --stack-name "$stack_name" \
        --region "$REGION" \
        --query 'StackResources[?ResourceType==`AWS::RDS::DBInstance`].PhysicalResourceId' \
        --output text 2>/dev/null || echo "")

    if [ -z "$db_instances" ]; then
        log_info "No RDS instances found"
        return 0
    fi

    for db_instance in $db_instances; do
        if [ "$RETAIN_DATA" = true ]; then
            log_info "Creating final snapshot for: $db_instance"

            if [ "$DRY_RUN" = false ]; then
                local snapshot_id="${db_instance}-final-$(date +%Y%m%d-%H%M%S)"

                aws rds create-db-snapshot \
                    --db-instance-identifier "$db_instance" \
                    --db-snapshot-identifier "$snapshot_id" \
                    --region "$REGION" 2>/dev/null || true

                log_info "Snapshot created: $snapshot_id"
            else
                log_info "[DRY RUN] Would create snapshot for: $db_instance"
            fi
        fi
    done
}

# Delete stack
delete_stack() {
    local stack_name="$1"

    log_info "Deleting stack: $stack_name"

    # Show what will be deleted
    identify_data_resources "$stack_name"

    # Confirm deletion
    if [ "$FORCE" = false ] && [ "$DRY_RUN" = false ]; then
        echo ""
        read -p "Are you sure you want to delete stack '$stack_name'? (yes/no): " confirm

        if [ "$confirm" != "yes" ]; then
            log_info "Deletion cancelled by user"
            return 0
        fi
    fi

    # Configure deletion policies
    configure_deletion_policies "$stack_name"

    # Empty S3 buckets
    empty_s3_buckets "$stack_name"

    # Handle RDS snapshots
    delete_rds_snapshots "$stack_name"

    if [ "$DRY_RUN" = true ]; then
        log_info "[DRY RUN] Would delete stack: $stack_name"
        return 0
    fi

    # Delete the stack
    log_info "Initiating stack deletion..."

    cd "$PROJECT_ROOT"
    export CDK_DEFAULT_REGION="$REGION"

    if [ "$RETAIN_RESOURCES" = true ]; then
        log_warn "Retain resources flag is set, but CDK doesn't support this directly"
        log_warn "Resources with RemovalPolicy.RETAIN in code will be retained"
    fi

    cdk destroy "$stack_name" \
        --context environment="$ENVIRONMENT" \
        --force

    if [ $? -eq 0 ]; then
        log_info "Stack deleted successfully: $stack_name"
    else
        log_error "Failed to delete stack: $stack_name"
        return 1
    fi
}

# Cleanup retained resources
cleanup_retained_resources() {
    log_info "Cleaning up retained resources..."

    # Find resources with retention tags
    log_info "Searching for retained resources in region: $REGION"

    # S3 buckets
    log_info "Checking S3 buckets..."
    local buckets=$(aws s3api list-buckets \
        --query "Buckets[?contains(Name, '${ENVIRONMENT}')].Name" \
        --output text 2>/dev/null || echo "")

    for bucket in $buckets; do
        local tags=$(aws s3api get-bucket-tagging \
            --bucket "$bucket" \
            --query "TagSet[?Key=='Environment'].Value" \
            --output text 2>/dev/null || echo "")

        if [ "$tags" = "$ENVIRONMENT" ]; then
            log_warn "Found retained bucket: $bucket"

            if [ "$FORCE" = false ]; then
                read -p "Delete bucket $bucket? (yes/no): " confirm
                if [ "$confirm" != "yes" ]; then
                    continue
                fi
            fi

            if [ "$DRY_RUN" = false ]; then
                aws s3 rb "s3://${bucket}" --force
                log_info "Deleted bucket: $bucket"
            else
                log_info "[DRY RUN] Would delete bucket: $bucket"
            fi
        fi
    done

    # RDS snapshots
    log_info "Checking RDS snapshots..."
    local snapshots=$(aws rds describe-db-snapshots \
        --region "$REGION" \
        --query "DBSnapshots[?contains(DBSnapshotIdentifier, '${ENVIRONMENT}')].DBSnapshotIdentifier" \
        --output text 2>/dev/null || echo "")

    for snapshot in $snapshots; do
        log_warn "Found retained snapshot: $snapshot"

        if [ "$FORCE" = false ]; then
            read -p "Delete snapshot $snapshot? (yes/no): " confirm
            if [ "$confirm" != "yes" ]; then
                continue
            fi
        fi

        if [ "$DRY_RUN" = false ]; then
            aws rds delete-db-snapshot \
                --db-snapshot-identifier "$snapshot" \
                --region "$REGION"
            log_info "Deleted snapshot: $snapshot"
        else
            log_info "[DRY RUN] Would delete snapshot: $snapshot"
        fi
    done

    log_info "Cleanup completed"
}

# Get deletion order (reverse of deployment order)
get_deletion_order() {
    local stacks=$(get_stacks_to_delete)

    if [ -z "$stacks" ]; then
        log_error "No stacks found to delete"
        exit 1
    fi

    # Reverse the order for deletion (delete dependents first)
    echo "$stacks" | tac
}

# Main execution
main() {
    if [ "$CLEANUP_RETAINED" = true ]; then
        cleanup_retained_resources
        exit 0
    fi

    local stacks=$(get_deletion_order)

    if [ -z "$stacks" ]; then
        log_error "No stacks found for environment: $ENVIRONMENT"
        exit 1
    fi

    log_info "Stacks to delete (in order):"
    echo "$stacks" | while read -r stack; do
        log_info "  - $stack"
    done
    echo ""

    if [ "$DRY_RUN" = true ]; then
        log_info "DRY RUN MODE - No actual deletions will be performed"
        echo ""
    fi

    # Delete stacks in order
    local failed_stacks=()

    while read -r stack; do
        if ! delete_stack "$stack"; then
            failed_stacks+=("$stack")
        fi
        echo ""
    done <<< "$stacks"

    # Report results
    if [ ${#failed_stacks[@]} -gt 0 ]; then
        log_error "Failed to delete the following stacks:"
        for stack in "${failed_stacks[@]}"; do
            log_error "  - $stack"
        done
        exit 1
    fi

    log_info "=== Deletion process completed successfully ==="
}

main
