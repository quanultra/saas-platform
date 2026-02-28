using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.AutoScaling;
using Constructs;
using System.Collections.Generic;
using EC2 = Amazon.CDK.AWS.EC2;
using RDS = Amazon.CDK.AWS.RDS;

namespace AwsSapC02Practice.Infrastructure.Constructs.DisasterRecovery
{
    public class PilotLightProps
    {
        public IVpc? Vpc { get; set; }
        public string? PrimaryDbEndpoint { get; set; }
        public string? PrimaryDbSecretArn { get; set; }
        public string DatabaseName { get; set; } = "sapc02db";
        public bool CreateReadReplica { get; set; } = true;
    }

    public class PilotLight : Construct
    {
        public IDatabaseInstance StandbyDatabase { get; }
        public Function FailoverFunction { get; }
        public AutoScalingGroup MinimalAsg { get; }

        public PilotLight(Construct scope, string id, PilotLightProps props)
            : base(scope, id)
        {
            var dbSecurityGroup = new SecurityGroup(this, "StandbyDbSG", new SecurityGroupProps
            {
                Vpc = props.Vpc,
                Description = "Security group for standby database in DR region",
                AllowAllOutbound = true
            });

            StandbyDatabase = new DatabaseInstance(this, "StandbyDb", new DatabaseInstanceProps
            {
                Engine = DatabaseInstanceEngine.Mysql(new MySqlInstanceEngineProps
                {
                    Version = MysqlEngineVersion.VER_8_0_40
                }),
                InstanceType = EC2.InstanceType.Of(EC2.InstanceClass.BURSTABLE3, EC2.InstanceSize.SMALL),
                Vpc = props.Vpc,
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
                SecurityGroups = new[] { dbSecurityGroup },
                DatabaseName = props.DatabaseName,
                AllocatedStorage = 20,
                MaxAllocatedStorage = 100,
                StorageType = StorageType.GP3,
                StorageEncrypted = true,
                BackupRetention = Duration.Days(7),
                PreferredBackupWindow = "03:00-04:00",
                PreferredMaintenanceWindow = "sun:04:00-sun:05:00",
                DeletionProtection = false,
                RemovalPolicy = RemovalPolicy.DESTROY,
                CloudwatchLogsExports = new[] { "error", "general", "slowquery" }
            });

            var failoverRole = new Role(this, "FailoverRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                Description = "Role for DR failover automation",
                ManagedPolicies = new IManagedPolicy[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaVPCAccessExecutionRole")
                }
            });

            failoverRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[] { "rds:*", "autoscaling:*", "ec2:DescribeInstances", "route53:*" },
                Resources = new[] { "*" }
            }));

            FailoverFunction = new Function(this, "FailoverFunction", new FunctionProps
            {
                Runtime = Runtime.PYTHON_3_11,
                Handler = "index.handler",
                Code = Code.FromInline("import boto3\nimport json\nimport os\n\ndef handler(event, context):\n    return {'statusCode': 200, 'body': json.dumps('Failover initiated')}"),
                Role = failoverRole,
                Timeout = Duration.Minutes(5),
                Environment = new Dictionary<string, string>
                {
                    { "DB_INSTANCE_ID", StandbyDatabase.InstanceIdentifier },
                    { "REGION", Stack.Of(this).Region }
                },
                Description = "Automated failover function for Pilot Light DR"
            });

            var asgSecurityGroup = new SecurityGroup(this, "AsgSG", new SecurityGroupProps
            {
                Vpc = props.Vpc,
                Description = "Security group for DR Auto Scaling Group",
                AllowAllOutbound = true
            });

            asgSecurityGroup.AddIngressRule(Peer.Ipv4(props.Vpc!.VpcCidrBlock), Port.Tcp(80), "Allow HTTP from VPC");

            var launchTemplate = new LaunchTemplate(this, "LaunchTemplate", new LaunchTemplateProps
            {
                InstanceType = EC2.InstanceType.Of(EC2.InstanceClass.T3, EC2.InstanceSize.MICRO),
                MachineImage = MachineImage.LatestAmazonLinux2(),
                SecurityGroup = asgSecurityGroup,
                UserData = UserData.ForLinux()
            });

            launchTemplate.UserData?.AddCommands(
                "#!/bin/bash",
                "yum update -y",
                "yum install -y httpd",
                "systemctl start httpd",
                "systemctl enable httpd",
                "echo '<h1>DR Instance - Pilot Light</h1>' > /var/www/html/index.html"
            );

            MinimalAsg = new AutoScalingGroup(this, "MinimalAsg", new AutoScalingGroupProps
            {
                Vpc = props.Vpc,
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                LaunchTemplate = launchTemplate,
                MinCapacity = 0,
                MaxCapacity = 10,
                DesiredCapacity = 0
            });

            FailoverFunction.AddEnvironment("ASG_NAME", MinimalAsg.AutoScalingGroupName);

            Tags.Of(this).Add("Component", "DisasterRecovery");
            Tags.Of(this).Add("DRStrategy", "PilotLight");
            Tags.Of(this).Add("RTO", "1-hour");
            Tags.Of(this).Add("RPO", "15-minutes");
        }
    }
}
