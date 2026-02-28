using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Constructs.Network
{
    public class TransitGatewayConstructProps
    {
        public required string Environment { get; set; }
        public string Description { get; set; } = "Transit Gateway for multi-VPC connectivity";
        public int AmazonSideAsn { get; set; } = 64512;
        public bool EnableDnsSupport { get; set; } = true;
        public bool EnableDefaultRouteTableAssociation { get; set; } = true;
        public bool EnableDefaultRouteTablePropagation { get; set; } = true;
    }

    /// <summary>
    /// Transit Gateway construct for connecting multiple VPCs
    /// Implements Requirements 6.3, 6.4
    /// </summary>
    public class TransitGatewayConstruct : Construct
    {
        public CfnTransitGateway TransitGateway { get; }
        public string TransitGatewayId { get; }
        public Dictionary<string, CfnTransitGatewayAttachment> VpcAttachments { get; }
        public CfnTransitGatewayRouteTable RouteTable { get; }

        public TransitGatewayConstruct(Construct scope, string id, TransitGatewayConstructProps props)
            : base(scope, id)
        {
            VpcAttachments = new Dictionary<string, CfnTransitGatewayAttachment>();

            // Create Transit Gateway
            TransitGateway = new CfnTransitGateway(this, "TransitGateway", new CfnTransitGatewayProps
            {
                Description = props.Description,
                AmazonSideAsn = props.AmazonSideAsn,
                DnsSupport = props.EnableDnsSupport ? "enable" : "disable",
                DefaultRouteTableAssociation = props.EnableDefaultRouteTableAssociation ? "enable" : "disable",
                DefaultRouteTablePropagation = props.EnableDefaultRouteTablePropagation ? "enable" : "disable"
            });
            Tags.Of(TransitGateway).Add("Name", $"{props.Environment}-transit-gateway");
            Tags.Of(TransitGateway).Add("Environment", props.Environment);

            TransitGatewayId = TransitGateway.Ref;

            // Create custom route table for more control
            RouteTable = new CfnTransitGatewayRouteTable(this, "RouteTable", new CfnTransitGatewayRouteTableProps
            {
                TransitGatewayId = TransitGatewayId
            });
            Tags.Of(RouteTable).Add("Name", $"{props.Environment}-tgw-route-table");

            // Add tags
            Tags.Of(this).Add("Component", "TransitGateway");
            Tags.Of(this).Add("Environment", props.Environment);
        }

        /// <summary>
        /// Attach a VPC to the Transit Gateway
        /// </summary>
        public CfnTransitGatewayAttachment AttachVpc(
            string attachmentId,
            IVpc vpc,
            string[] subnetIds,
            bool enableDnsSupport = true)
        {
            var attachment = new CfnTransitGatewayAttachment(this, attachmentId, new CfnTransitGatewayAttachmentProps
            {
                TransitGatewayId = TransitGatewayId,
                VpcId = vpc.VpcId,
                SubnetIds = subnetIds
            });
            Tags.Of(attachment).Add("Name", $"{attachmentId}-attachment");

            VpcAttachments[attachmentId] = attachment;

            // Associate with route table
            var association = new CfnTransitGatewayRouteTableAssociation(
                this,
                $"{attachmentId}Association",
                new CfnTransitGatewayRouteTableAssociationProps
                {
                    TransitGatewayAttachmentId = attachment.Ref,
                    TransitGatewayRouteTableId = RouteTable.Ref
                });

            // Enable route propagation
            var propagation = new CfnTransitGatewayRouteTablePropagation(
                this,
                $"{attachmentId}Propagation",
                new CfnTransitGatewayRouteTablePropagationProps
                {
                    TransitGatewayAttachmentId = attachment.Ref,
                    TransitGatewayRouteTableId = RouteTable.Ref
                });

            return attachment;
        }

        /// <summary>
        /// Add a route to the Transit Gateway route table
        /// </summary>
        public void AddRoute(string routeId, string destinationCidr, string attachmentId)
        {
            if (!VpcAttachments.ContainsKey(attachmentId))
            {
                throw new System.ArgumentException($"Attachment {attachmentId} not found");
            }

            new CfnTransitGatewayRoute(this, routeId, new CfnTransitGatewayRouteProps
            {
                TransitGatewayRouteTableId = RouteTable.Ref,
                DestinationCidrBlock = destinationCidr,
                TransitGatewayAttachmentId = VpcAttachments[attachmentId].Ref
            });
        }
    }
}
