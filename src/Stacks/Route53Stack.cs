using Amazon.CDK;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.CloudWatch;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    public class Route53Stack : BaseStack
    {
        public IHostedZone HostedZone { get; }
        public CfnHealthCheck PrimaryHealthCheck { get; }
        public CfnHealthCheck SecondaryHealthCheck { get; }

        public Route53Stack(
            Construct scope,
            string id,
            IStackProps props,
            StackConfiguration config,
            Route53StackProps route53Props)
            : base(scope, id, props, config)
        {
            HostedZone = CreateHostedZone(route53Props.DomainName);
            PrimaryHealthCheck = CreateHealthCheck("PrimaryHealthCheck", route53Props.PrimaryEndpoint, route53Props.PrimaryRegion);
            SecondaryHealthCheck = CreateHealthCheck("SecondaryHealthCheck", route53Props.SecondaryEndpoint, route53Props.SecondaryRegion);
            CreateFailoverRecords(route53Props.DomainName, route53Props.PrimaryEndpoint, route53Props.SecondaryEndpoint);
            CreateHealthCheckAlarms();
            CreateOutputs();
        }

        private IHostedZone CreateHostedZone(string domainName)
        {
            var zone = new PublicHostedZone(this, "HostedZone", new PublicHostedZoneProps
            {
                ZoneName = domainName,
                Comment = $"Hosted zone for {Config.ProjectName} multi-region application"
            });
            Amazon.CDK.Tags.Of(zone).Add("Component", "Route53");
            Amazon.CDK.Tags.Of(zone).Add("Purpose", "GlobalRouting");
            return zone;
        }

        private CfnHealthCheck CreateHealthCheck(string id, string endpoint, string region)
        {
            return new CfnHealthCheck(this, id, new CfnHealthCheckProps
            {
                HealthCheckConfig = new CfnHealthCheck.HealthCheckConfigProperty
                {
                    Type = "HTTPS",
                    ResourcePath = "/health",
                    FullyQualifiedDomainName = endpoint,
                    Port = 443,
                    RequestInterval = 30,
                    FailureThreshold = 3,
                    MeasureLatency = true,
                    EnableSni = true
                },

                HealthCheckTags = new[]
                {
                    new CfnHealthCheck.HealthCheckTagProperty { Key = "Name", Value = $"{Config.ProjectName}-{region}-health-check" },
                    new CfnHealthCheck.HealthCheckTagProperty { Key = "Region", Value = region },
                    new CfnHealthCheck.HealthCheckTagProperty { Key = "Environment", Value = Config.Environment }
                }
            });
        }

        private void CreateFailoverRecords(string domainName, string primaryEndpoint, string secondaryEndpoint)
        {
            new CfnRecordSet(this, "PrimaryRecord", new CfnRecordSetProps
            {
                HostedZoneId = HostedZone.HostedZoneId,
                Name = domainName,
                Type = "A",
                SetIdentifier = "Primary",
                Failover = "PRIMARY",
                HealthCheckId = PrimaryHealthCheck.AttrHealthCheckId,
                AliasTarget = new CfnRecordSet.AliasTargetProperty
                {
                    DnsName = primaryEndpoint,
                    HostedZoneId = "Z35SXDOTRQ7X7K",
                    EvaluateTargetHealth = true
                }
            });
            new CfnRecordSet(this, "SecondaryRecord", new CfnRecordSetProps
            {
                HostedZoneId = HostedZone.HostedZoneId,
                Name = domainName,
                Type = "A",
                SetIdentifier = "Secondary",
                Failover = "SECONDARY",
                HealthCheckId = SecondaryHealthCheck.AttrHealthCheckId,
                AliasTarget = new CfnRecordSet.AliasTargetProperty
                {
                    DnsName = secondaryEndpoint,
                    HostedZoneId = "Z32O12XQLNTSW2",
                    EvaluateTargetHealth = true
                }
            });
        }


        private void CreateHealthCheckAlarms()
        {
            new Alarm(this, "PrimaryHealthCheckAlarm", new AlarmProps
            {
                Metric = new Metric(new MetricProps
                {
                    Namespace = "AWS/Route53",
                    MetricName = "HealthCheckStatus",
                    DimensionsMap = new System.Collections.Generic.Dictionary<string, string> { ["HealthCheckId"] = PrimaryHealthCheck.AttrHealthCheckId },
                    Statistic = "Minimum",
                    Period = Duration.Minutes(1)
                }),
                Threshold = 1,
                EvaluationPeriods = 2,
                ComparisonOperator = ComparisonOperator.LESS_THAN_THRESHOLD,
                AlarmDescription = "Alert when primary endpoint health check fails",
                AlarmName = GenerateResourceName("primary-health-alarm"),
                TreatMissingData = TreatMissingData.BREACHING
            });
            new Alarm(this, "SecondaryHealthCheckAlarm", new AlarmProps
            {
                Metric = new Metric(new MetricProps
                {
                    Namespace = "AWS/Route53",
                    MetricName = "HealthCheckStatus",
                    DimensionsMap = new System.Collections.Generic.Dictionary<string, string> { ["HealthCheckId"] = SecondaryHealthCheck.AttrHealthCheckId },
                    Statistic = "Minimum",
                    Period = Duration.Minutes(1)
                }),
                Threshold = 1,
                EvaluationPeriods = 2,
                ComparisonOperator = ComparisonOperator.LESS_THAN_THRESHOLD,
                AlarmDescription = "Alert when secondary endpoint health check fails",
                AlarmName = GenerateResourceName("secondary-health-alarm"),
                TreatMissingData = TreatMissingData.BREACHING
            });
        }

        private void CreateOutputs()
        {
            CreateOutput("HostedZoneId", HostedZone.HostedZoneId, "Route 53 Hosted Zone ID");
            CreateOutput("HostedZoneName", HostedZone.ZoneName, "Route 53 Hosted Zone Name");
            CreateOutput("PrimaryHealthCheckId", PrimaryHealthCheck.AttrHealthCheckId, "Primary Health Check ID");
            CreateOutput("SecondaryHealthCheckId", SecondaryHealthCheck.AttrHealthCheckId, "Secondary Health Check ID");
            CreateOutput("NameServers", Fn.Join(",", HostedZone.HostedZoneNameServers ?? new string[0]), "Name Servers");
        }
    }

    public class Route53StackProps
    {
        public string DomainName { get; set; } = "";
        public string PrimaryEndpoint { get; set; } = "";
        public string SecondaryEndpoint { get; set; } = "";
        public string PrimaryRegion { get; set; } = "";
        public string SecondaryRegion { get; set; } = "";
    }
}
