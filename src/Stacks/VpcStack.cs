using Amazon.CDK;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Constructs.Network;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// VPC Stack with multi-region support
    /// Implements Requirements 3.1, 3.2, 8.1
    /// </summary>
    public class VpcStack : BaseStack
    {
        public MultiRegionVpc PrimaryVpc { get; }
        public MultiRegionVpc? SecondaryVpc { get; private set; }

        public VpcStack(Construct scope, string id, IStackProps props, StackConfiguration config)
            : base(scope, id, props, config)
        {
            // Create primary VPC
            PrimaryVpc = new MultiRegionVpc(this, "PrimaryVpc", new MultiRegionVpcProps
            {
                Environment = config.Environment,
                Region = config.MultiRegion.PrimaryRegion,
                CidrBlock = config.Network.PrimaryCidr,
                MaxAzs = config.Network.MaxAzs,
                EnableNatGateway = config.Network.EnableNatGateway,
                NatGateways = 2 // High availability with 2 NAT Gateways
            });

            // Create outputs for primary VPC
            CreateVpcOutputs("Primary", PrimaryVpc, config.MultiRegion.PrimaryRegion);
        }

        /// <summary>
        /// Create secondary VPC for multi-region deployment
        /// </summary>
        public void CreateSecondaryVpc(Construct scope, StackConfiguration config)
        {
            SecondaryVpc = new MultiRegionVpc(scope, "SecondaryVpc", new MultiRegionVpcProps
            {
                Environment = config.Environment,
                Region = config.MultiRegion.SecondaryRegion,
                CidrBlock = config.Network.SecondaryCidr,
                MaxAzs = config.Network.MaxAzs,
                EnableNatGateway = config.Network.EnableNatGateway,
                NatGateways = 2
            });

            // Create outputs for secondary VPC
            CreateVpcOutputs("Secondary", SecondaryVpc, config.MultiRegion.SecondaryRegion);
        }

        /// <summary>
        /// Create CloudFormation outputs for VPC resources
        /// </summary>
        private void CreateVpcOutputs(string prefix, MultiRegionVpc vpc, string region)
        {
            CreateOutput(
                $"{prefix}VpcId",
                vpc.Vpc.VpcId,
                $"{prefix} VPC ID in {region}"
            );

            CreateOutput(
                $"{prefix}VpcCidr",
                vpc.Vpc.VpcCidrBlock,
                $"{prefix} VPC CIDR block"
            );

            CreateOutput(
                $"{prefix}AppSecurityGroupId",
                vpc.ApplicationSecurityGroup.SecurityGroupId,
                $"{prefix} Application Security Group ID"
            );

            CreateOutput(
                $"{prefix}DbSecurityGroupId",
                vpc.DatabaseSecurityGroup.SecurityGroupId,
                $"{prefix} Database Security Group ID"
            );

            CreateOutput(
                $"{prefix}LbSecurityGroupId",
                vpc.LoadBalancerSecurityGroup.SecurityGroupId,
                $"{prefix} Load Balancer Security Group ID"
            );
        }
    }
}
