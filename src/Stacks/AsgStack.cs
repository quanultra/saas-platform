using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.CloudWatch;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// Auto Scaling Group Stack with launch templates and scaling policies
    /// Implements Requirements 8.2, 8.8
    /// </summary>
    public class AsgStack : BaseStack
    {
        public Amazon.CDK.AWS.AutoScaling.AutoScalingGroup AutoScalingGroup { get; }

        public AsgStack(
            Construct scope,
            string id,
            IStackProps props,
            StackConfiguration config,
            IVpc vpc,
            ISecurityGroup appSecurityGroup,
            IApplicationTargetGroup targetGroup)
            : base(scope, id, props, config)
        {
            // Create Launch Template
            var launchTemplate = new LaunchTemplate(this, "LaunchTemplate", new LaunchTemplateProps
            {
                LaunchTemplateName = GenerateResourceName("lt"),
                InstanceType = new Amazon.CDK.AWS.EC2.InstanceType("t3.micro"),
                MachineImage = MachineImage.LatestAmazonLinux2023(),
                SecurityGroup = appSecurityGroup,
                UserData = UserData.ForLinux()
            });

            // Add user data script
            launchTemplate.UserData?.AddCommands(
                "#!/bin/bash",
                "yum update -y",
                "yum install -y httpd",
                "systemctl start httpd",
                "systemctl enable httpd",
                "echo '<h1>Hello from Auto Scaling Group</h1>' > /var/www/html/index.html",
                "mkdir -p /var/www/html",
                "echo 'OK' > /var/www/html/health"
            );

            // Create Auto Scaling Group
            AutoScalingGroup = new Amazon.CDK.AWS.AutoScaling.AutoScalingGroup(this, "ASG", new AutoScalingGroupProps
            {
                Vpc = vpc,
                LaunchTemplate = launchTemplate,
                MinCapacity = 2,
                MaxCapacity = 10,
                DesiredCapacity = 2,
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS }
            });

            // Attach to target group
            targetGroup.AddTarget(AutoScalingGroup);

            // Add target tracking scaling policy
            AutoScalingGroup.ScaleOnCpuUtilization("CpuScaling", new CpuUtilizationScalingProps
            {
                TargetUtilizationPercent = 70,
                Cooldown = Duration.Seconds(300)
            });

            // Add step scaling policy for rapid scale-out
            var cpuMetric = new Metric(new MetricProps
            {
                Namespace = "AWS/EC2",
                MetricName = "CPUUtilization",
                DimensionsMap = new Dictionary<string, string>
                {
                    ["AutoScalingGroupName"] = AutoScalingGroup.AutoScalingGroupName
                },
                Statistic = "Average",
                Period = Duration.Seconds(60)
            });

            var stepScalingPolicy = AutoScalingGroup.ScaleOnMetric("StepScaling", new BasicStepScalingPolicyProps
            {
                Metric = cpuMetric,
                ScalingSteps = new[]
                {
                    new ScalingInterval { Lower = 0, Upper = 50, Change = 0 },
                    new ScalingInterval { Lower = 50, Upper = 70, Change = 1 },
                    new ScalingInterval { Lower = 70, Upper = 85, Change = 2 },
                    new ScalingInterval { Lower = 85, Change = 3 }
                },
                AdjustmentType = AdjustmentType.CHANGE_IN_CAPACITY,
                Cooldown = Duration.Seconds(60)
            });

            // Add lifecycle hooks
            AutoScalingGroup.AddLifecycleHook("LaunchHook", new BasicLifecycleHookProps
            {
                LifecycleTransition = LifecycleTransition.INSTANCE_LAUNCHING,
                DefaultResult = DefaultResult.CONTINUE,
                HeartbeatTimeout = Duration.Seconds(300)
            });

            AutoScalingGroup.AddLifecycleHook("TerminateHook", new BasicLifecycleHookProps
            {
                LifecycleTransition = LifecycleTransition.INSTANCE_TERMINATING,
                DefaultResult = DefaultResult.CONTINUE,
                HeartbeatTimeout = Duration.Seconds(300)
            });

            // Create outputs
            CreateOutput("AutoScalingGroupName", AutoScalingGroup.AutoScalingGroupName, "ASG Name");
            CreateOutput("LaunchTemplateId", launchTemplate.LaunchTemplateId ?? "N/A", "Launch Template ID");
        }
    }

    /// <summary>
    /// Props for AsgStack - used by tests
    /// </summary>
    public class AsgStackProps : StackProps
    {
        public string Environment { get; set; } = "test";
        public int MinCapacity { get; set; } = 2;
        public int DesiredCapacity { get; set; } = 2;
        public int MaxCapacity { get; set; } = 10;
    }
}
