using Amazon.CDK;
using Amazon.CDK.AWS.WAFv2;
using Constructs;

namespace AwsSapC02Practice.Infrastructure.Stacks;

public class WafStack : Stack
{
    public CfnWebACL WebAcl { get; }

    public WafStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        WebAcl = new CfnWebACL(this, "WebAcl", new CfnWebACLProps
        {
            Scope = "CLOUDFRONT",
            DefaultAction = new CfnWebACL.DefaultActionProperty
            {
                Allow = new CfnWebACL.AllowActionProperty { }
            },
            VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
            {
                CloudWatchMetricsEnabled = true,
                MetricName = "WebAclMetric",
                SampledRequestsEnabled = true
            },
            Rules = new[]
            {
                new CfnWebACL.RuleProperty
                {
                    Name = "AWSManagedRulesCommonRuleSet",
                    Priority = 1,
                    Statement = new CfnWebACL.StatementProperty
                    {
                        ManagedRuleGroupStatement = new CfnWebACL.ManagedRuleGroupStatementProperty
                        {
                            VendorName = "AWS",
                            Name = "AWSManagedRulesCommonRuleSet"
                        }
                    },
                    OverrideAction = new CfnWebACL.OverrideActionProperty
                    {
                        None = new { }
                    },
                    VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
                    {
                        CloudWatchMetricsEnabled = true,
                        MetricName = "CommonRuleSetMetric",
                        SampledRequestsEnabled = true
                    }
                },
                new CfnWebACL.RuleProperty
                {
                    Name = "RateLimitRule",
                    Priority = 2,
                    Statement = new CfnWebACL.StatementProperty
                    {
                        RateBasedStatement = new CfnWebACL.RateBasedStatementProperty
                        {
                            Limit = 2000,
                            AggregateKeyType = "IP"
                        }
                    },
                    Action = new CfnWebACL.RuleActionProperty
                    {
                        Block = new CfnWebACL.BlockActionProperty { }
                    },
                    VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
                    {
                        CloudWatchMetricsEnabled = true,
                        MetricName = "RateLimitMetric",
                        SampledRequestsEnabled = true
                    }
                },
                new CfnWebACL.RuleProperty
                {
                    Name = "GeoBlockingRule",
                    Priority = 3,
                    Statement = new CfnWebACL.StatementProperty
                    {
                        GeoMatchStatement = new CfnWebACL.GeoMatchStatementProperty
                        {
                            CountryCodes = new[] { "CN", "RU", "KP" }
                        }
                    },
                    Action = new CfnWebACL.RuleActionProperty
                    {
                        Block = new CfnWebACL.BlockActionProperty { }
                    },
                    VisibilityConfig = new CfnWebACL.VisibilityConfigProperty
                    {
                        CloudWatchMetricsEnabled = true,
                        MetricName = "GeoBlockingMetric",
                        SampledRequestsEnabled = true
                    }
                }
            }
        });

        new CfnOutput(this, "WebAclArn", new CfnOutputProps
        {
            Value = WebAcl.AttrArn,
            ExportName = "WafWebAclArn"
        });

        Amazon.CDK.Tags.Of(this).Add("Component", "Security");
        Amazon.CDK.Tags.Of(this).Add("Service", "WAF");
    }
}
