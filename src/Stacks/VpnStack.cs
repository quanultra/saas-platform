using Amazon.CDK;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Constructs.Network;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// VPN Stack for Site-to-Site VPN connectivity
    /// Implements Requirements 6.1, 6.2
    /// </summary>
    public class VpnStack : BaseStack
    {
        public SiteToSiteVpn SiteToSiteVpn { get; }

        public VpnStack(
            Construct scope,
            string id,
            IStackProps props,
            StackConfiguration config,
            MultiRegionVpc vpc,
            string customerGatewayIp = "203.0.113.1") // Default placeholder IP
            : base(scope, id, props, config)
        {
            // Create Site-to-Site VPN
            SiteToSiteVpn = new SiteToSiteVpn(this, "SiteToSiteVpn", new SiteToSiteVpnProps
            {
                Vpc = vpc.Vpc,
                CustomerGatewayIp = customerGatewayIp,
                CustomerGatewayAsn = 65000,
                EnableBgp = true,
                Environment = config.Environment
            });

            // Create outputs
            CreateOutput(
                "CustomerGatewayId",
                SiteToSiteVpn.CustomerGateway.Ref,
                "Customer Gateway ID"
            );

            CreateOutput(
                "VirtualPrivateGatewayId",
                SiteToSiteVpn.VirtualPrivateGateway.Ref,
                "Virtual Private Gateway ID"
            );

            CreateOutput(
                "VpnConnectionId",
                SiteToSiteVpn.VpnConnectionId,
                "VPN Connection ID"
            );
        }
    }
}
