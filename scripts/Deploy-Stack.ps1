# AWS SAP-C02 Practice Infrastructure - Stack Deployment Script (PowerShell)
# This script handles CDK stack deployment with error handling, dependencies, and rollback

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,

    [Parameter(Mandatory=$false)]
    [string]$Stack,

    [Parameter(Mandatory=$false)]
    [string]$Region = 'us-east-1',

    [Parameter(Mandatory=$false)]
    [string]$Account,

    [Parameter(Mandatory=$false)]
    [switch]$DryRun,

    [Parameter(Mandatory=$false)]
    [switch]$SkipTests,

    [Parameter(Mandatory=$false)]
    [switch]$AutoApprove,

    [Parameter(Mandatory=$false)]
    [switch]$RollbackOnFailure
)

# Set error action preference
$ErrorActionPreference = 'Stop'

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$LogDir = Join-Path $ProjectRoot 'logs'
$Timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$DeploymentLog = Join-Path $LogDir "deployment-$Timestamp.log"

# Create log directory
if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir | Out-Null
}

# Logging functions
function Write-Log {
    param(
        [string]$Message,
        [ValidateSet('Info', 'Warning', 'Error')]
        [string]$Level = 'Info'
    )

    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timestamp] [$Level] $Message"

    switch ($Level) {
        'Info'    { Write-Host $logMessage -ForegroundColor Green }
        'Warning' { Write-Host $logMessage -ForegroundColor Yellow }
        'Error'   { Write-Host $logMessage -ForegroundColor Red }
    }

    Add-Content -Path $DeploymentLog -Value $logMessage
}

# Get AWS account ID
function Get-AwsAccountId {
    try {
        $identity = aws sts get-caller-identity --query Account --output text 2>&1
        if ($LASTEXITCODE -eq 0) {
            return $identity.Trim()
        }
        return $null
    }
    catch {
        return $null
    }
}

# Check prerequisites
function Test-Prerequisites {
    Write-Log "Checking prerequisites..."

    # Check AWS CLI
    if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
        Write-Log "AWS CLI is not installed" -Level Error
        exit 1
    }

    # Check CDK CLI
    if (-not (Get-Command cdk -ErrorAction SilentlyContinue)) {
        Write-Log "AWS CDK CLI is not installed. Run: npm install -g aws-cdk" -Level Error
        exit 1
    }

    # Check .NET SDK
    if (-not (Get-Command dotnet -ErrorAction SilentlyConti
) {
            throw "Failed to restore dependencies"
        }

        dotnet build --configuration Release
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to build project"
        }

        Write-Log "Build completed successfully"
    }
    finally {
        Pop-Location
    }
}

# Run tests
function Invoke-Tests {
    if ($SkipTests) {
        Write-Log "Skipping tests as requested" -Level Warning
        return
    }

    Write-Log "Running tests..."

    Push-Location $ProjectRoot

    try {
        dotnet test --configuration Release --no-build
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed"
        }

        Write-Log "All tests passed"
    }
    finally {
        Pop-Location
    }
}

# Bootstrap CDK
function Initialize-CdkBootstrap {
    Write-Log "Checking CDK bootstrap status..."

    $stackExists = aws cloudformation describe-stacks --stack-name CDKToolkit --region $Region 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Log "CDK already bootstrapped in $Region"
    }
    else {
        Write-Log "Bootstrapping CDK in $Region..."
        cdk bootstrap "aws://$Account/$Region"

        if ($LASTEXITCODE -ne 0) {
            throw "Failed to bootstrap CDK"
        }

        Write-Log "CDK bootstrap completed"
    }
}

# Synthesize stacks
function Invoke-CdkSynth {
    Write-Log "Synthesizing CloudFormation templates..."

    Push-Location $ProjectRoot

    try {
        $env:CDK_DEFAULT_ACCOUNT = $Account
        $env:CDK_DEFAULT_REGION = $Region

        if ($Stack) {
            cdk synth $Stack --context environment=$Environment
        }
        else {
            cdk synth --context environment=$Environment
        }

        if ($LASTEXITCODE -ne 0) {
            throw "Failed to synthesize stacks"
        }

        Write-Log "Synthesis completed successfully"
    }
    finally {
        Pop-Location
    }
}

# Deploy stacks
function Invoke-CdkDeploy {
    Write-Log "Deploying stacks..."

    Push-Location $ProjectRoot

    try {
        $env:CDK_DEFAULT_ACCOUNT = $Account
        $env:CDK_DEFAULT_REGION = $Region

        $deployArgs = @('deploy')

        if ($Stack) {
            $deployArgs += $Stack
        }
        else {
            $deployArgs += '--all'
        }

        $deployArgs += '--context', "environment=$Environment"

        if ($AutoApprove) {
            $deployArgs += '--require-approval', 'never'
        }

        if ($RollbackOnFailure) {
            $deployArgs += '--rollback', 'true'
        }
        else {
            $deployArgs += '--rollback', 'false'
        }

        Write-Log "Executing: cdk $($deployArgs -join ' ')"

        & cdk @deployArgs

        if ($LASTEXITCODE -ne 0) {
            throw "Deployment failed"
        }

        Write-Log "Deployment completed successfully"
    }
    catch {
        Write-Log "Deployment failed: $_" -Level Error

        if ($RollbackOnFailure) {
            Write-Log "Rollback was enabled, CloudFormation will automatically rollback changes" -Level Warning
        }

        throw
    }
    finally {
        Pop-Location
    }
}

# Verify deployment
function Test-Deployment {
    Write-Log "Verifying deployment..."

    $stacksToVerify = @()

    if ($Stack) {
        $stacksToVerify = @($Stack)
    }
    else {
        # Get all stacks
        Push-Location $ProjectRoot
        $env:CDK_DEFAULT_ACCOUNT = $Account
        $env:CDK_DEFAULT_REGION = $Region

        $stackList = cdk list --context environment=$Environment 2>&1
        if ($LASTEXITCODE -eq 0) {
            $stacksToVerify = $stackList -split "`n" | Where-Object { $_ -match '\S' }
        }
        Pop-Location
    }

    if ($stacksToVerify.Count -eq 0) {
        Write-Log "No stacks found to verify" -Level Warning
        return
    }

    $failedStacks = @()

    foreach ($stackName in $stacksToVerify) {
        Write-Log "Verifying stack: $stackName"

        $stackStatus = aws cloudformation describe-stacks `
            --stack-name $stackName `
            --region $Region `
            --query 'Stacks[0].StackStatus' `
            --output text 2>&1

        if ($LASTEXITCODE -eq 0) {
            if ($stackStatus -match 'CREATE_COMPLETE|UPDATE_COMPLETE') {
                Write-Log "Stack $stackName is in healthy state: $stackStatus"
            }
            else {
                Write-Log "Stack $stackName is in unexpected state: $stackStatus" -Level Error
                $failedStacks += $stackName
            }
        }
        else {
            Write-Log "Failed to get status for stack: $stackName" -Level Error
            $failedStacks += $stackName
        }
    }

    if ($failedStacks.Count -gt 0) {
        throw "The following stacks failed verification: $($failedStacks -join ', ')"
    }

    Write-Log "All stacks verified successfully"
}

# Main execution
try {
    Write-Log "=== AWS CDK Stack Deployment ==="
    Write-Log "Log file: $DeploymentLog"

    # Get account ID if not provided
    if (-not $Account) {
        $Account = Get-AwsAccountId
        if (-not $Account) {
            Write-Log "Failed to get AWS account ID. Please provide it with -Account parameter" -Level Error
            exit 1
        }
    }

    Write-Log "Starting deployment process..."
    Write-Log "Environment: $Environment"
    Write-Log "Region: $Region"
    Write-Log "Account: $Account"
    Write-Log "Stack: $(if ($Stack) { $Stack } else { 'all' })"

    Test-Prerequisites
    Build-Project
    Invoke-Tests
    Initialize-CdkBootstrap
    Invoke-CdkSynth

    if ($DryRun) {
        Write-Log "Dry run completed. Skipping actual deployment."
        Write-Log "Review synthesized templates in cdk.out/ directory"
        exit 0
    }

    Invoke-CdkDeploy
    Test-Deployment

    Write-Log "=== Deployment completed successfully ==="
    Write-Log "Environment: $Environment"
    Write-Log "Region: $Region"
    Write-Log "Timestamp: $(Get-Date)"
}
catch {
    Write-Log "Deployment process failed: $_" -Level Error
    exit 1
}
