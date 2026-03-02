using Amazon.CDK;
using System;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Stacks;

namespace AwsSapC02Practice.Infrastructure
{
    /// <summary>
    /// Main CDK Application Entry Point
    /// Implements Requirement 1.1
    /// </summary>
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            // Get environment from context or environment variable
            var environmentName = app.Node.TryGetContext("environment")?.ToString()
                ?? System.Environment.GetEnvironmentVariable("ENVIRONMENT")
                ?? "dev";

            // Load environment-specific configuration
            var envConfig = EnvironmentConfig.GetConfig(environmentName);

            // Create stack configuration
            var config = CreateStackConfiguration(envConfig, environmentName);

            // Create stack props for primary and secondary regions
            var primaryProps = CreateStackProps(envConfig.AwsAccount, envConfig.PrimaryRegion);
            var secondaryProps = CreateStackProps(envConfig.AwsAccount, envConfig.SecondaryRegion);

            // Initialize integration manager
            var integrationManager = new StackIntegrationManager(app, config);

            // Deploy all stacks in phases
            DeployPhase1CoreInfrastructure(app, config, primaryProps, integrationManager);
            DeployPhase2SecurityInfrastructure(app, config, primaryProps, integrationManager);
            DeployPhase3StorageInfrastructure(app, config, primaryProps, integrationManager);
            DeployPhase4DatabaseInfrastructure(app, config, primaryProps, integrationManager);
            DeployPhase5ComputeInfrastructure(app, config, primaryProps, integrationManager);
            DeployPhase6ServerlessInfrastructure(app, config, primaryProps, integrationManager);
            DeployPhase7DisasterRecovery(app, config, primaryProps, secondaryProps, integrationManager);
            DeployPhase8MonitoringObservability(app, config, primaryProps, integrationManager);

            // Synthesize the CDK app
            app.Synth();
        }


        private static StackConfiguration CreateStackConfiguration(EnvironmentConfig envConfig, string environmentName)
        {
            return new StackConfiguration
            {
                Environment = envConfig.EnvironmentName,
                ProjectName = "aws-sap-c02-practice",
                Tags = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["Owner"] = "SAP-C02-Student",
                    ["Purpose"] = "Training",
                    ["CostCenter"] = "Training"
                },
                Network = new NetworkConfiguration
                {
                    PrimaryCidr = "10.0.0.0/16",
                    SecondaryCidr = "10.1.0.0/16",
                    MaxAzs = 3,
                    EnableNatGateway = true
                },
                Database = new DatabaseConfiguration
                {
                    Engine = "aurora-postgresql",
                    InstanceClass = envConfig.ResourceSizing.RdsInstanceClass,
                    EnableEncryption = true,
                    BackupRetentionDays = environmentName == "prod" ? 30 : 7,
                    DatabaseName = envConfig.ParameterStoreValues.GetValueOrDefault(
                        $"/aws-sap-c02/{environmentName}/db-name", "sapc02db")
                },
                Security = new SecurityConfiguration
                {
                    EnableWaf = true,
                    EnableGuardDuty = true,
                    EnableSecurityHub = true,
                    AllowedCidrs = new System.Collections.Generic.List<string>()
                },
                MultiRegion = new MultiRegionConfig
                {
                    PrimaryRegion = envConfig.PrimaryRegion,
                    SecondaryRegion = envConfig.SecondaryRegion,
                    EnableCrossRegionReplication = environmentName != "dev"
                },
                Monitoring = new MonitoringConfiguration
                {
                    AlarmEmail = envConfig.ParameterStoreValues.GetValueOrDefault(
                        $"/aws-sap-c02/{environmentName}/alarm-email", "alerts@example.com"),
                    EnableXRay = true,
                    EnableContainerInsights = true,
                    LogRetentionDays = int.Parse(envConfig.ParameterStoreValues.GetValueOrDefault(
                        $"/aws-sap-c02/{environmentName}/log-retention-days", "30"))
                }
            };
        }

        private static StackProps CreateStackProps(string account, string region)
        {
            return new StackProps
            {
                Env = new Amazon.CDK.Environment
                {
                    Account = account,
                    Region = region
                }
            };
        }


        private static void DeployPhase1CoreInfrastructure(
            App app, StackConfiguration config, StackProps primaryProps, StackIntegrationManager integrationManager)
        {
            // 1. VPC Stack
            var vpcStack = new VpcStack(app, $"{config.ProjectName}-{config.Environment}-vpc", primaryProps, config);
            integrationManager.RegisterStack("vpc", vpcStack);

            // 2. Transit Gateway Stack
            var transitGatewayStack = new TransitGatewayStack(
                app, $"{config.ProjectName}-{config.Environment}-tgw", primaryProps, config);
            integrationManager.RegisterStack("transitGateway", transitGatewayStack);
            transitGatewayStack.AddDependency(vpcStack);
            integrationManager.WireVpcsWithTransitGateway(vpcStack, transitGatewayStack);

            // 3. VPN Stack
            var vpnStack = new VpnStack(
                app, $"{config.ProjectName}-{config.Environment}-vpn", primaryProps, config, vpcStack.PrimaryVpc);
            integrationManager.RegisterStack("vpn", vpnStack);
            vpnStack.AddDependency(vpcStack);
        }

        private static void DeployPhase2SecurityInfrastructure(
            App app, StackConfiguration config, StackProps primaryProps, StackIntegrationManager integrationManager)
        {
            // 4. KMS Stack
            var kmsStack = new KmsStack(app, $"{config.ProjectName}-{config.Environment}-kms", primaryProps);
            integrationManager.RegisterStack("kms", kmsStack);

            // 5. WAF Stack
            var wafStack = new WafStack(app, $"{config.ProjectName}-{config.Environment}-waf", primaryProps);
            integrationManager.RegisterStack("waf", wafStack);

            // 6. CloudTrail Stack
            var cloudTrailStack = new CloudTrailStack(
                app, $"{config.ProjectName}-{config.Environment}-cloudtrail", primaryProps);
            integrationManager.RegisterStack("cloudTrail", cloudTrailStack);
            cloudTrailStack.AddDependency(kmsStack);

            // 7. Security Monitoring Stack
            var securityMonitoringStack = new SecurityMonitoringStack(
                app, $"{config.ProjectName}-{config.Environment}-security-monitoring", primaryProps);
            integrationManager.RegisterStack("securityMonitoring", securityMonitoringStack);
        }

        private static void DeployPhase3StorageInfrastructure(
            App app, StackConfiguration config, StackProps primaryProps, StackIntegrationManager integrationManager)
        {
            var kmsStack = integrationManager.GetStack<KmsStack>("kms");
            var wafStack = integrationManager.GetStack<WafStack>("waf");

            // 8. S3 Stack
            var s3Stack = new S3Stack(app, $"{config.ProjectName}-{config.Environment}-s3", primaryProps, config);
            integrationManager.RegisterStack("s3", s3Stack);
            s3Stack.AddDependency(kmsStack);

            // 9. CloudFront Stack
            var cloudFrontStack = new CloudFrontStack(
                app, $"{config.ProjectName}-{config.Environment}-cloudfront", primaryProps, config, s3Stack.CrossRegionStorage.PrimaryBucket);
            integrationManager.RegisterStack("cloudFront", cloudFrontStack);
            cloudFrontStack.AddDependency(s3Stack);
            cloudFrontStack.AddDependency(wafStack);
        }


        private static void DeployPhase4DatabaseInfrastructure(
            App app, StackConfiguration config, StackProps primaryProps, StackIntegrationManager integrationManager)
        {
            var vpcStack = integrationManager.GetStack<VpcStack>("vpc");
            var kmsStack = integrationManager.GetStack<KmsStack>("kms");

            // 10. RDS Stack
            var rdsStack = new RdsStack(
                app, $"{config.ProjectName}-{config.Environment}-rds", primaryProps, config,
                vpcStack.PrimaryVpc.Vpc, vpcStack.PrimaryVpc.DatabaseSecurityGroup);
            integrationManager.RegisterStack("rds", rdsStack);
            rdsStack.AddDependency(vpcStack);
            rdsStack.AddDependency(kmsStack);

            // 11. Aurora Stack
            var auroraStack = new AuroraStack(
                app, $"{config.ProjectName}-{config.Environment}-aurora", primaryProps, config,
                vpcStack.PrimaryVpc.Vpc, vpcStack.PrimaryVpc.DatabaseSecurityGroup);
            integrationManager.RegisterStack("aurora", auroraStack);
            auroraStack.AddDependency(vpcStack);
            auroraStack.AddDependency(kmsStack);

            // 12. ElastiCache Stack
            var elastiCacheStack = new ElastiCacheStack(
                app, $"{config.ProjectName}-{config.Environment}-elasticache", primaryProps, config, vpcStack.PrimaryVpc.Vpc, vpcStack.PrimaryVpc.DatabaseSecurityGroup);
            integrationManager.RegisterStack("elastiCache", elastiCacheStack);
            elastiCacheStack.AddDependency(vpcStack);

            // 13. DynamoDB Stack
            var dynamoDbStack = new DynamoDbStack(
                app, $"{config.ProjectName}-{config.Environment}-dynamodb", primaryProps, config);
            integrationManager.RegisterStack("dynamoDb", dynamoDbStack);
            dynamoDbStack.AddDependency(kmsStack);
        }

        private static void DeployPhase5ComputeInfrastructure(
            App app, StackConfiguration config, StackProps primaryProps, StackIntegrationManager integrationManager)
        {
            var vpcStack = integrationManager.GetStack<VpcStack>("vpc");

            // 14. ALB Stack
            var albStack = new AlbStack(
                app, $"{config.ProjectName}-{config.Environment}-alb", primaryProps, config,
                vpcStack.PrimaryVpc.Vpc, vpcStack.PrimaryVpc.LoadBalancerSecurityGroup);
            integrationManager.RegisterStack("alb", albStack);
            albStack.AddDependency(vpcStack);

            // 15. ASG Stack
            var asgStack = new AsgStack(
                app, $"{config.ProjectName}-{config.Environment}-asg", primaryProps, config,
                vpcStack.PrimaryVpc.Vpc, vpcStack.PrimaryVpc.ApplicationSecurityGroup, albStack.TargetGroup);
            integrationManager.RegisterStack("asg", asgStack);
            asgStack.AddDependency(vpcStack);
            asgStack.AddDependency(albStack);

            // 16. ECS Stack
            var ecsStack = new EcsStack(
                app, $"{config.ProjectName}-{config.Environment}-ecs", primaryProps, config,
                vpcStack.PrimaryVpc.Vpc, vpcStack.PrimaryVpc.ApplicationSecurityGroup);
            integrationManager.RegisterStack("ecs", ecsStack);
            ecsStack.AddDependency(vpcStack);

            // 17. EKS Stack
            var eksStack = new EksStack(
                app, $"{config.ProjectName}-{config.Environment}-eks", primaryProps, config, vpcStack.PrimaryVpc.Vpc);
            integrationManager.RegisterStack("eks", eksStack);
            eksStack.AddDependency(vpcStack);

            // 18. App Mesh Stack
            var appMeshStack = new AppMeshStack(
                app, $"{config.ProjectName}-{config.Environment}-appmesh", primaryProps, config, ecsStack.ServiceNamespace);
            integrationManager.RegisterStack("appMesh", appMeshStack);
        }


        private static void DeployPhase6ServerlessInfrastructure(
            App app, StackConfiguration config, StackProps primaryProps, StackIntegrationManager integrationManager)
        {
            var vpcStack = integrationManager.GetStack<VpcStack>("vpc");
            var wafStack = integrationManager.GetStack<WafStack>("waf");

            // 19. Serverless Stack
            var serverlessStack = new ServerlessStack(
                app, $"{config.ProjectName}-{config.Environment}-serverless", primaryProps, config);
            integrationManager.RegisterStack("serverless", serverlessStack);
            serverlessStack.AddDependency(vpcStack);

            // 20. API Gateway Stack
            var apiGatewayStack = new ApiGatewayStack(
                app, $"{config.ProjectName}-{config.Environment}-apigateway", primaryProps, config);
            integrationManager.RegisterStack("apiGateway", apiGatewayStack);
            apiGatewayStack.AddDependency(wafStack);

            // 21. Step Functions Stack
            var stepFunctionsStack = new StepFunctionsStack(
                app, $"{config.ProjectName}-{config.Environment}-stepfunctions", primaryProps, config);
            integrationManager.RegisterStack("stepFunctions", stepFunctionsStack);

            // 22. EventBridge Stack
            var eventBridgeStack = new EventBridgeStack(
                app, $"{config.ProjectName}-{config.Environment}-eventbridge", primaryProps, config);
            integrationManager.RegisterStack("eventBridge", eventBridgeStack);
        }

        private static void DeployPhase7DisasterRecovery(
            App app, StackConfiguration config, StackProps primaryProps, StackProps secondaryProps,
            StackIntegrationManager integrationManager)
        {
            var rdsStack = integrationManager.GetStack<RdsStack>("rds");
            var dynamoDbStack = integrationManager.GetStack<DynamoDbStack>("dynamoDb");
            var albStack = integrationManager.GetStack<AlbStack>("alb");

            // 23. Backup Stack
            var backupStack = new BackupStack(
                app, $"{config.ProjectName}-{config.Environment}-backup", primaryProps, config);
            integrationManager.RegisterStack("backup", backupStack);
            backupStack.AddDependency(rdsStack);
            backupStack.AddDependency(dynamoDbStack);

            // 24. Pilot Light Stack
            var pilotLightStack = new PilotLightStack(
                app, $"{config.ProjectName}-{config.Environment}-pilotlight", secondaryProps, config);
            integrationManager.RegisterStack("pilotLight", pilotLightStack);

            // 25. Warm Standby Stack
            var warmStandbyStack = new WarmStandbyStack(
                app, $"{config.ProjectName}-{config.Environment}-warmstandby", secondaryProps, config);
            integrationManager.RegisterStack("warmStandby", warmStandbyStack);

            var route53Props = new Route53StackProps
            {
                DomainName = "example.com",
                PrimaryEndpoint = albStack.LoadBalancer.LoadBalancerDnsName,
                SecondaryEndpoint = "secondary-alb.example.com",
                PrimaryRegion = config.MultiRegion.PrimaryRegion,
                SecondaryRegion = config.MultiRegion.SecondaryRegion
            };
            var route53Stack = new Route53Stack(
                app, $"{config.ProjectName}-{config.Environment}-route53", primaryProps, config, route53Props);
            integrationManager.RegisterStack("route53", route53Stack);
            route53Stack.AddDependency(albStack);
        }


        private static void DeployPhase8MonitoringObservability(
            App app, StackConfiguration config, StackProps primaryProps, StackIntegrationManager integrationManager)
        {
            var ecsStack = integrationManager.GetStack<EcsStack>("ecs");
            var eksStack = integrationManager.GetStack<EksStack>("eks");

            // 27. Monitoring Stack
            var monitoringStack = new MonitoringStack(
                app, $"{config.ProjectName}-{config.Environment}-monitoring", config, primaryProps);
            integrationManager.RegisterStack("monitoring", monitoringStack);

            // 28. CloudWatch Logs Stack
            var cloudWatchLogsStack = new CloudWatchLogsStack(
                app, $"{config.ProjectName}-{config.Environment}-logs", config, primaryProps);
            integrationManager.RegisterStack("cloudWatchLogs", cloudWatchLogsStack);

            // 29. X-Ray Stack
            var xrayStack = new XRayStack(
                app, $"{config.ProjectName}-{config.Environment}-xray", primaryProps, config);
            integrationManager.RegisterStack("xray", xrayStack);

            // 30. Container Insights Stack
            var containerInsightsStack = new ContainerInsightsStack(
                app, $"{config.ProjectName}-{config.Environment}-container-insights", primaryProps, config);
            integrationManager.RegisterStack("containerInsights", containerInsightsStack);
            containerInsightsStack.AddDependency(ecsStack);
            containerInsightsStack.AddDependency(eksStack);

            // Wire monitoring with all resources
            integrationManager.WireMonitoringWithResources(monitoringStack);
        }
    }
}
