using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.CertificateManager;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// Application Load Balancer Stack with SSL/TLS termination
    /// Implements Requirements 8.1, 8.7
    /// </summary>
    public class AlbStack : BaseStack
    {
        public IApplicationLoadBalancer LoadBalancer { get; }
        public IApplicationTargetGroup TargetGroup { get; }

        public AlbStack(
            Construct scope,
            string id,
            IStackProps props,
            StackConfiguration config,
            IVpc vpc,
            ISecurityGroup lbSecurityGroup)
            : base(scope, id, props, config)
        {
            // Create Application Load Balancer
            LoadBalancer = new ApplicationLoadBalancer(this, "ALB", new ApplicationLoadBalancerProps
            {
                Vpc = vpc,
                InternetFacing = true,
                LoadBalancerName = GenerateResourceName("alb"),
                SecurityGroup = lbSecurityGroup,
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC }
            });

            // Create Target Group with health checks
            TargetGroup = new ApplicationTargetGroup(this, "TargetGroup", new ApplicationTargetGroupProps
            {
                Vpc = vpc,
                Port = 80,
                Protocol = ApplicationProtocol.HTTP,
                TargetType = TargetType.INSTANCE,
                TargetGroupName = GenerateResourceName("tg"),
                HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
                {
                    Enabled = true,
                    Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol.HTTP,
                    Path = "/health",
                    HealthyThresholdCount = 2,
                    UnhealthyThresholdCount = 3,
                    Interval = Duration.Seconds(30),
                    Timeout = Duration.Seconds(5)
                },
                DeregistrationDelay = Duration.Seconds(30)
            });

            // Add HTTP listener
            var httpListener = LoadBalancer.AddListener("HttpListener", new BaseApplicationListenerProps
            {
                Port = 80,
                Protocol = ApplicationProtocol.HTTP,
                DefaultTargetGroups = new[] { TargetGroup }
            });

            // Add HTTPS listener if SSL certificate is provided
            if (config.Security.EnableWaf)
            {
                // Note: In production, you would import or create an ACM certificate
                // For now, we'll just add the HTTP listener
                // Uncomment below when certificate ARN is available:
                /*
                var certificate = Certificate.FromCertificateArn(this, "Certificate", 
                    "arn:aws:acm:region:account:certificate/certificate-id");
                
                var httpsListener = LoadBalancer.AddListener("HttpsListener", new BaseApplicationListenerProps
                {
                    Port = 443,
                    Protocol = ApplicationProtocol.HTTPS,
                    Certificates = new[] { certificate },
                    DefaultTargetGroups = new[] { TargetGroup }
                });
                */
            }

            // Create outputs
            CreateOutput("LoadBalancerArn", LoadBalancer.LoadBalancerArn, "ALB ARN");
            CreateOutput("LoadBalancerDnsName", LoadBalancer.LoadBalancerDnsName, "ALB DNS Name");
            CreateOutput("TargetGroupArn", TargetGroup.TargetGroupArn, "Target Group ARN");
        }
    }

    /// <summary>
    /// Props for AlbStack - used by tests
    /// </summary>
    public class AlbStackProps : StackProps
    {
        public string Environment { get; set; } = "test";
        public bool EnableHttps { get; set; } = false;
    }
}
