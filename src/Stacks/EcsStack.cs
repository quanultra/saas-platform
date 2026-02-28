using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.ServiceDiscovery;
using Amazon.CDK.AWS.Logs;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// ECS Fargate Stack with service discovery
    /// Implements Requirement 8.10
    /// </summary>
    public class EcsStack : BaseStack
    {
        public ICluster Cluster { get; }
        public FargateService FargateService { get; }
        public IPrivateDnsNamespace ServiceNamespace { get; }
        public IApplicationTargetGroup TargetGroup { get; }

        public EcsStack(
            Construct scope,
            string id,
            IStackProps props,
            StackConfiguration config,
            IVpc vpc,
            ISecurityGroup appSecurityGroup)
            : base(scope, id, props, config)
        {
            // Create ECS Cluster
            Cluster = new Cluster(this, "EcsCluster", new ClusterProps
            {
                ClusterName = GenerateResourceName("ecs-cluster"),
                Vpc = vpc,
                ContainerInsights = true,
                EnableFargateCapacityProviders = true
            });

            // Create CloudWatch Log Group
            var logGroup = new LogGroup(this, "ServiceLogGroup", new LogGroupProps
            {
                LogGroupName = $"/ecs/{GenerateResourceName("service")}",
                Retention = RetentionDays.ONE_WEEK,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // Create Task Definition
            var taskDefinition = new FargateTaskDefinition(this, "TaskDef", new FargateTaskDefinitionProps
            {
                MemoryLimitMiB = 512,
                Cpu = 256,
                RuntimePlatform = new RuntimePlatform
                {
                    CpuArchitecture = CpuArchitecture.X86_64,
                    OperatingSystemFamily = OperatingSystemFamily.LINUX
                }
            });

            // Add Container to Task Definition
            var container = taskDefinition.AddContainer("WebContainer", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromRegistry("amazon/amazon-ecs-sample"),
                MemoryLimitMiB = 512,
                Cpu = 256,
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    StreamPrefix = "ecs",
                    LogGroup = logGroup
                }),
                Environment = new Dictionary<string, string>
                {
                    ["ENVIRONMENT"] = config.Environment,
                    ["PROJECT"] = config.ProjectName
                }
            });

            // Add port mapping
            container.AddPortMappings(new PortMapping
            {
                ContainerPort = 80,
                Protocol = Amazon.CDK.AWS.ECS.Protocol.TCP
            });

            // Create Service Discovery Namespace
            ServiceNamespace = new PrivateDnsNamespace(this, "ServiceNamespace", new PrivateDnsNamespaceProps
            {
                Name = $"{config.ProjectName}.local",
                Vpc = vpc,
                Description = "Service discovery namespace for ECS services"
            });

            // Create ALB Target Group
            TargetGroup = new ApplicationTargetGroup(this, "TargetGroup", new ApplicationTargetGroupProps
            {
                Vpc = vpc,
                Port = 80,
                Protocol = ApplicationProtocol.HTTP,
                TargetType = TargetType.IP,
                HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
                {
                    Path = "/",
                    Interval = Duration.Seconds(30),
                    Timeout = Duration.Seconds(5),
                    HealthyThresholdCount = 2,
                    UnhealthyThresholdCount = 3
                },
                DeregistrationDelay = Duration.Seconds(30)
            });

            // Create Fargate Service
            FargateService = new FargateService(this, "FargateService", new FargateServiceProps
            {
                Cluster = Cluster,
                TaskDefinition = taskDefinition,
                DesiredCount = 2,
                AssignPublicIp = false,
                SecurityGroups = new[] { appSecurityGroup },
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                ServiceName = GenerateResourceName("fargate-service"),
                CloudMapOptions = new CloudMapOptions
                {
                    Name = "web-service",
                    CloudMapNamespace = ServiceNamespace,
                    DnsRecordType = DnsRecordType.A,
                    DnsTtl = Duration.Seconds(60)
                },
                EnableExecuteCommand = true,
                CircuitBreaker = new DeploymentCircuitBreaker
                {
                    Rollback = true
                }
            });

            // Attach Fargate Service to Target Group
            FargateService.AttachToApplicationTargetGroup(TargetGroup);

            // Configure Auto Scaling
            var scaling = FargateService.AutoScaleTaskCount(new Amazon.CDK.AWS.ApplicationAutoScaling.EnableScalingProps
            {
                MinCapacity = 2,
                MaxCapacity = 10
            });

            scaling.ScaleOnCpuUtilization("CpuScaling", new CpuUtilizationScalingProps
            {
                TargetUtilizationPercent = 70,
                ScaleInCooldown = Duration.Seconds(60),
                ScaleOutCooldown = Duration.Seconds(60)
            });

            scaling.ScaleOnMemoryUtilization("MemoryScaling", new MemoryUtilizationScalingProps
            {
                TargetUtilizationPercent = 80,
                ScaleInCooldown = Duration.Seconds(60),
                ScaleOutCooldown = Duration.Seconds(60)
            });

            // Create Outputs
            CreateOutput("ClusterName", Cluster.ClusterName, "ECS Cluster Name");
            CreateOutput("ClusterArn", Cluster.ClusterArn, "ECS Cluster ARN");
            CreateOutput("ServiceName", FargateService.ServiceName, "Fargate Service Name");
            CreateOutput("ServiceArn", FargateService.ServiceArn, "Fargate Service ARN");
            CreateOutput("ServiceNamespaceId", ServiceNamespace.NamespaceId, "Service Discovery Namespace ID");
            CreateOutput("ServiceNamespaceName", ServiceNamespace.NamespaceName, "Service Discovery Namespace Name");
            CreateOutput("TargetGroupArn", TargetGroup.TargetGroupArn, "Target Group ARN");
        }
    }
}
