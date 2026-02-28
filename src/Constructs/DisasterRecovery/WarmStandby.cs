using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.Route53;
using Constructs;

using RDS = Amazon.CDK.AWS.RDS;
using EC2 = Amazon.CDK.AWS.EC2;
namespace AwsSapC02Practice.Infrastructure.Constructs.DisasterRecovery
{
    public class WarmStandbyProps
    {
        public IVpc Vpc { get; set; }
        public string DatabaseName { get; set; } = "sapc02db";
        public int MinCapacity { get; set; } = 1;
        public int MaxCapacity { get; set; } = 10;
        public int DesiredCapacity { get; set; } = 2;
        public IHostedZone HostedZone { get; set; }
        public string DomainName { get; set; }
    }

    public class WarmStandby : Construct
    {
        public IDatabaseInstance StandbyDatabase { get; }
        public IApplicationLoadBalancer LoadBalancer { get; }
        public AutoScalingGroup AutoScalingGroup { get; }
        public IApplicationTargetGroup TargetGroup { get; }

        public WarmStandby(Construct scope, string id, WarmStandbyProps props)
            : base(scope, id)
        {
            var dbSecurityGroup = new SecurityGroup(this, "WarmStandbyDbSG", new SecurityGroupProps
            {
                Vpc = props.Vpc,
                Description = "Security group for warm standby database",
                AllowAllOutbound = true
            });

            var appSecurityGroup = new SecurityGroup(this, "WarmStandbyAppSG", new SecurityGroupProps
            {
                Vpc = props.Vpc,
                Description = "Security group for warm standby application tier",
                AllowAllOutbound = true
            });

            dbSecurityGroup.AddIngressRule(appSecurityGroup, Port.Tcp(3306), "Allow MySQL from application tier");

            StandbyDatabase = new DatabaseInstance(this, "WarmStandbyDb", new DatabaseInstanceProps
            {
                Engine = DatabaseInstanceEngine.Mysql(new MySqlInstanceEngineProps
                {
                    Version = MysqlEngineVersion.VER_8_0_40
                }),
                InstanceType = EC2.InstanceType.Of(EC2.InstanceClass.BURSTABLE3, EC2.InstanceSize.MEDIUM),
                Vpc = props.Vpc,
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
                SecurityGroups = new[] { dbSecurityGroup },
                DatabaseName = props.DatabaseName,
                MultiAz = true,
                AllocatedStorage = 50,
                MaxAllocatedStorage = 200,
                StorageType = StorageType.GP3,
                StorageEncrypted = true,
                BackupRetention = Duration.Days(7),
                PreferredBackupWindow = "03:00-04:00",
                PreferredMaintenanceWindow = "sun:04:00-sun:05:00",
                DeletionProtection = false,
                RemovalPolicy = RemovalPolicy.DESTROY,
                CloudwatchLogsExports = new[] { "error", "general", "slowquery" },
                EnablePerformanceInsights = true,
                PerformanceInsightRetention = PerformanceInsightRetention.DEFAULT
            });

            var albSecurityGroup = new SecurityGroup(this, "AlbSG", new SecurityGroupProps
            {
                Vpc = props.Vpc,
                Description = "Security group for warm standby ALB",
                AllowAllOutbound = true
            });

            LoadBalancer = new ApplicationLoadBalancer(this, "WarmStandbyALB", new ApplicationLoadBalancerProps
            {
                Vpc = props.Vpc,
                InternetFacing = true,
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
                SecurityGroup = albSecurityGroup
            });

            LoadBalancer.Connections.AllowFromAnyIpv4(Port.Tcp(80), "Allow HTTP from internet");
            LoadBalancer.Connections.AllowFromAnyIpv4(Port.Tcp(443), "Allow HTTPS from internet");

            TargetGroup = new ApplicationTargetGroup(this, "WarmStandbyTG", new ApplicationTargetGroupProps
            {
                Vpc = props.Vpc,
                Port = 80,
                Protocol = ApplicationProtocol.HTTP,
                TargetType = TargetType.INSTANCE,
                HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
                {
                    Path = "/health",
                    Interval = Duration.Seconds(30),
                    Timeout = Duration.Seconds(5),
                    HealthyThresholdCount = 2,
                    UnhealthyThresholdCount = 3
                },
                DeregistrationDelay = Duration.Seconds(30)
            });

            LoadBalancer.AddListener("HttpListener", new ApplicationListenerProps
            {
                Port = 80,
                Protocol = ApplicationProtocol.HTTP,
                DefaultTargetGroups = new[] { TargetGroup }
            });

            appSecurityGroup.AddIngressRule(LoadBalancer.Connections.SecurityGroups[0], Port.Tcp(80), "Allow HTTP from ALB");

            var launchTemplate = new LaunchTemplate(this, "WarmStandbyLT", new LaunchTemplateProps
            {
                InstanceType = EC2.InstanceType.Of(EC2.InstanceClass.T3, EC2.InstanceSize.SMALL),
                MachineImage = MachineImage.LatestAmazonLinux2(),
                SecurityGroup = appSecurityGroup,
                UserData = UserData.ForLinux()
            });

            launchTemplate.UserData?.AddCommands(
                "#!/bin/bash",
                "yum update -y",
                "yum install -y httpd",
                "systemctl start httpd",
                "systemctl enable httpd",
                "echo '<h1>Warm Standby DR Instance</h1>' > /var/www/html/index.html",
                "echo 'OK' > /var/www/html/health"
            );

            AutoScalingGroup = new AutoScalingGroup(this, "WarmStandbyAsg", new AutoScalingGroupProps
            {
                Vpc = props.Vpc,
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                LaunchTemplate = launchTemplate,
                MinCapacity = props.MinCapacity,
                MaxCapacity = props.MaxCapacity,
                DesiredCapacity = props.DesiredCapacity
            });

            AutoScalingGroup.AttachToApplicationTargetGroup(TargetGroup);

            AutoScalingGroup.ScaleOnCpuUtilization("CpuScaling", new CpuUtilizationScalingProps
            {
                TargetUtilizationPercent = 70
            });


            Tags.Of(this).Add("Component", "DisasterRecovery");
            Tags.Of(this).Add("DRStrategy", "WarmStandby");
            Tags.Of(this).Add("RTO", "30-minutes");
            Tags.Of(this).Add("RPO", "5-minutes");
        }
    }
}
