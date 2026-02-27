using Amazon.CDK;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Constructs.Network;
using System.Linq;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// Transit Gateway Stack for multi-VPC connectivity
    /// Implements Requirements 6.3, 6.4
    /// </summary>
    public class TransitGatewayStack : BaseStack
    {
        public TransitGatewayConstruct TransitGateway { get; }

        public TransitGatewayStack(
            Construct scope,
            string id,
            IStackProps props,
            StackConfiguration config)
            : base(scope, id, props, config)
        {
            // Create Transit Gateway
            TransitGateway = new TransitGatewayConstruct(this, "TransitGateway", new TransitGatewayConstructProps
            {
                Environment = config.Environment,
                Description = "Transit Gateway for AWS SAP-C02 Practice Infrastructure",
                AmazonSideAsn = 64512,
                EnableDnsSupport = true,
                EnableDefaultRouteTableAssociation = false, // Use custom route table
                EnableDefaultRouteTablePropagation = false
            });

            // Create outputs
            CreateOutput(
                "TransitGatewayId",
                TransitGateway.TransitGatewayId,
                "Transit Gateway ID"
            );

            CreateOutput(
                "TransitGatewayRouteTableId",
                TransitGateway.RouteTable.Ref,
                "Transit Gateway Route Table ID"
            );
        }

        /// <summary>
        /// Attach a VPC to the Transit Gateway
        /// </summary>
        public void AttachVpc(string name, MultiRegionVpc vpc)
        {
            // Get private subnet IDs for attachment
            var subnetIds = vpc.Vpc.PrivateSubnets
                .Select(subnet => subnet.SubnetId)
                .ToArray();

            var attachment = TransitGateway.AttachVpc(
                name,
                vpc.Vpc,
                subnetIds,
                enableDnsSupport: true
            );

            // Create output for attachment
            CreateOutput(
                $"{name}AttachmentId",
                attachment.Ref,
                $"Transit Gateway attachment ID for {name} VPC"
            );

            // Add routes to VPC route tables pointing to Transit Gateway
            AddVpcRoutesToTransitGateway(vpc, name);
        }

        /// <summary>
        /// Add routes in VPC route tables to route traffic through Transit Gateway
        /// </summary>
        private void AddVpcRoutesToTransitGateway(MultiRegionVpc vpc, string vpcName)
        {
            var privateSubnets = vpc.Vpc.PrivateSubnets;

            for (int i = 0; i < privateSubnets.Length; i++)
            {
                var subnet = privateSubnets[i];

                // Add route to Transit Gateway for cross-VPC communication
                // This is a placeholder CIDR - should be configured based on other VPC CIDRs
                new Amazon.CDK.AWS.EC2.CfnRoute(
                    this,
                    $"{vpcName}TgwRoute{i}",
                    new Amazon.CDK.AWS.EC2.CfnRouteProps
                    {
                        RouteTableId = subnet.RouteTable.RouteTableId,
                        DestinationCidrBlock = "10.0.0.0/8", // Route all private traffic through TGW
                        TransitGatewayId = TransitGateway.TransitGatewayId
                    });
            }
        }
    }
}
