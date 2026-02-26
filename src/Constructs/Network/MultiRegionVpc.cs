using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace AwsSapC02Practice.Infrastructure.Constructs.Network
{
public class MultiRegionVpcProps
    {
        public required string Environment { get; set; }
        public required string Region { get; set; }
        public required string CidrBlock { get; set; }
        public int MaxAzs { get; set; } = 3;
        public bool EnableNatGateway { get; set; } = true;
        public int NatGateways { get; set; } = 2;
    }


    public class MultiRegionVpc : Construct
    {
        public IVpc Vpc { get; }
        public ISecurityGroup ApplicationSecurityGroup { get; }
        public ISecurityGroup DatabaseSecurityGroup { get; }
        public ISecurityGroup LoadBalancerSecurityGroup { get; }

        public MultiRegionVpc(Construct scope, string id, MultiRegionVpcProps props)
            : base(scope, id)
        {
            Vpc = new Vpc(this, "Vpc", new VpcProps
            {
                IpAddresses = IpAddresses.Cidr(props.CidrBlock),
                MaxAzs = props.MaxAzs,
                NatGateways = props.EnableNatGateway ? props.NatGateways : 0,
                SubnetConfiguration = new[]
                {
                    new SubnetConfiguration
                    {
                        Name = "Public",
                        SubnetType = SubnetType.PUBLIC,
                        CidrMask = 24
                    },
                    new SubnetConfiguration
                    {
                        Name = "Private",
                        SubnetType = SubnetType.PRIVATE_WITH_EGRESS,
                        CidrMask = 24
                    },
                    new SubnetConfiguration
                    {
                        Name = "Isolated",
                        SubnetType = SubnetType.PRIVATE_ISOLATED,
                        CidrMask = 24
                    }
                },

                EnableDnsHostnames = true,
                EnableDnsSupport = true
            });

            ApplicationSecurityGroup = new SecurityGroup(this, "AppSG", new SecurityGroupProps
            {
                Vpc = Vpc,
                Description = "Security group for application tier",
                AllowAllOutbound = true
            });

            DatabaseSecurityGroup = new SecurityGroup(this, "DbSG", new SecurityGroupProps
            {
                Vpc = Vpc,
                Description = "Security group for database tier",
                AllowAllOutbound = false
            });

            LoadBalancerSecurityGroup = new SecurityGroup(this, "LbSG", new SecurityGroupProps
            {
                Vpc = Vpc,
                Description = "Security group for load balancer",
                AllowAllOutbound = true
            });

            ConfigureSecurityGroupRules();

            Tags.Of(Vpc).Add("Name", $"{props.Environment}-vpc-{props.Region}");
            Tags.Of(Vpc).Add("Environment", props.Environment);
            Tags.Of(Vpc).Add("Region", props.Region);
        }

        private void ConfigureSecurityGroupRules()
        {
            LoadBalancerSecurityGroup.AddIngressRule(
                Peer.AnyIpv4(),
                Port.Tcp(80),
                "Allow HTTP from internet"
            );
            LoadBalancerSecurityGroup.AddIngressRule(
                Peer.AnyIpv4(),
                Port.Tcp(443),
                "Allow HTTPS from internet"
            );

            ApplicationSecurityGroup.AddIngressRule(
                LoadBalancerSecurityGroup,
                Port.Tcp(80),
                "Allow HTTP from Load Balancer"
            );
            ApplicationSecurityGroup.AddIngressRule(
                LoadBalancerSecurityGroup,
                Port.Tcp(443),
                "Allow HTTPS from Load Balancer"
            );

            DatabaseSecurityGroup.AddIngressRule(
                ApplicationSecurityGroup,
                Port.Tcp(5432),
                "Allow PostgreSQL from Application tier"
            );
            DatabaseSecurityGroup.AddIngressRule(
                ApplicationSecurityGroup,
                Port.Tcp(3306),
                "Allow MySQL from Application tier"
            );
        }
    }
}
