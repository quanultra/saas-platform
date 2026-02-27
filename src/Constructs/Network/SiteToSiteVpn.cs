using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace AwsSapC02Practice.Infrastructure.Constructs.Network
{
    public class SiteToSiteVpnProps
    {
        public required IVpc Vpc { get; set; }
        public required string CustomerGatewayIp { get; set; }
        public int CustomerGatewayAsn { get; set; } = 65000;
        public bool EnableBgp { get; set; } = true;
        public string Environment { get; set; } = "dev";
    }

    /// <summary>
    /// Site-to-Site VPN construct with Customer Gateway and Virtual Private Gateway
    /// Implements Requirements 6.1, 6.2
    /// </summary>
    public class SiteToSiteVpn : Construct
    {
        public CfnCustomerGateway CustomerGateway { get; }
        public CfnVPNGateway VirtualPrivateGateway { get; }
        public CfnVPNConnection VpnConnection { get; }
        public string VpnConnectionId { get; }

        public SiteToSiteVpn(Construct scope, string id, SiteToSiteVpnProps props)
            : base(scope, id)
        {
            // Create Customer Gateway
            CustomerGateway = new CfnCustomerGateway(this, "CustomerGateway", new CfnCustomerGatewayProps
            {
                BgpAsn = props.CustomerGatewayAsn,
                IpAddress = props.CustomerGatewayIp,
                Type = "ipsec.1"
            });
            Tags.Of(CustomerGateway).Add("Name", $"{props.Environment}-customer-gateway");

            // Create Virtual Private Gateway
            VirtualPrivateGateway = new CfnVPNGateway(this, "VirtualPrivateGateway", new CfnVPNGatewayProps
            {
                Type = "ipsec.1",
                AmazonSideAsn = 64512
            });
            Tags.Of(VirtualPrivateGateway).Add("Name", $"{props.Environment}-virtual-private-gateway");

            // Attach VPN Gateway to VPC
            var vpcAttachment = new CfnVPCGatewayAttachment(this, "VpcGatewayAttachment", new CfnVPCGatewayAttachmentProps
            {
                VpcId = props.Vpc.VpcId,
                VpnGatewayId = VirtualPrivateGateway.Ref
            });

            // Create VPN Connection with BGP
            VpnConnection = new CfnVPNConnection(this, "VpnConnection", new CfnVPNConnectionProps
            {
                Type = "ipsec.1",
                CustomerGatewayId = CustomerGateway.Ref,
                VpnGatewayId = VirtualPrivateGateway.Ref,
                StaticRoutesOnly = !props.EnableBgp
            });
            Tags.Of(VpnConnection).Add("Name", $"{props.Environment}-vpn-connection");

            // Ensure VPN connection is created after VPC attachment
            VpnConnection.AddDependency(vpcAttachment);

            VpnConnectionId = VpnConnection.Ref;

            // Enable route propagation for private subnets
            EnableRoutePropagation(props.Vpc);

            // Add tags
            Tags.Of(this).Add("Component", "SiteToSiteVPN");
            Tags.Of(this).Add("Environment", props.Environment);
        }

        /// <summary>
        /// Enable route propagation for VPN Gateway on private route tables
        /// </summary>
        private void EnableRoutePropagation(IVpc vpc)
        {
            var privateSubnets = vpc.PrivateSubnets;

            for (int i = 0; i < privateSubnets.Length; i++)
            {
                var subnet = privateSubnets[i];
                new CfnVPNGatewayRoutePropagation(this, $"RoutePropagation{i}", new CfnVPNGatewayRoutePropagationProps
                {
                    RouteTableIds = new[] { subnet.RouteTable.RouteTableId },
                    VpnGatewayId = VirtualPrivateGateway.Ref
                });
            }
        }
    }
}
