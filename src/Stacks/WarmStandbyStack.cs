using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Route53;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Constructs.DisasterRecovery;
using AwsSapC02Practice.Infrastructure.Constructs.Network;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// Warm Standby DR Stack
    /// Implements scaled-down but running infrastructure in DR region
    /// Implements Requirements 7.3, 8.6
    /// </summary>
    public class WarmStandbyStack : BaseStack
    {
        public WarmStandby WarmStandby { get; }
        public IVpc DrVpc { get; }

        public WarmStandbyStack(Construct scope, string id, IStackProps props, StackConfiguration config, IHostedZone hostedZone = null)
            : base(scope, id, props, config)
        {
            // Create VPC in DR region
            DrVpc = new MultiRegionVpc(this, "WarmStandbyVpc", new MultiRegionVpcProps
            {
                Environment = config.Environment,
                Region = config.MultiRegion.SecondaryRegion,
                CidrBlock = config.Network.SecondaryCidr,
                MaxAzs = config.Network.MaxAzs
            }).Vpc;

            // Create Warm Standby infrastructure
            WarmStandby = new WarmStandby(this, "WarmStandby", new WarmStandbyProps
            {
                Vpc = DrVpc,
                DatabaseName = config.Database.DatabaseName,
                MinCapacity = 1,
                MaxCapacity = 10,
                DesiredCapacity = 2, // Scaled-down but running
                HostedZone = hostedZone
            });

            // Create CloudFormation outputs
            CreateOutput(
                "WarmStandbyVpcId",
                DrVpc.VpcId,
                "VPC ID for Warm Standby in DR region"
            );

            CreateOutput(
                "WarmStandbyDbEndpoint",
                WarmStandby.StandbyDatabase.DbInstanceEndpointAddress,
                "Warm Standby database endpoint"
            );

            CreateOutput(
                "WarmStandbyDbArn",
                WarmStandby.StandbyDatabase.InstanceArn,
                "ARN of Warm Standby database"
            );

            CreateOutput(
                "WarmStandbyAlbDns",
                WarmStandby.LoadBalancer.LoadBalancerDnsName,
                "DNS name of Warm Standby ALB"
            );

            CreateOutput(
                "WarmStandbyAlbArn",
                WarmStandby.LoadBalancer.LoadBalancerArn,
                "ARN of Warm Standby ALB"
            );

            CreateOutput(
                "WarmStandbyAsgName",
                WarmStandby.AutoScalingGroup.AutoScalingGroupName,
                "Name of Warm Standby Auto Scaling Group"
            );

            CreateOutput(
                "WarmStandbyTargetGroupArn",
                WarmStandby.TargetGroup.TargetGroupArn,
                "ARN of Warm Standby target group"
            );

            CreateOutput(
                "WarmStandbyRegion",
                config.MultiRegion.SecondaryRegion,
                "Warm Standby DR region"
            );
        }
    }
}
