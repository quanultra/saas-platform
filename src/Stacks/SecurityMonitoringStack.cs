using Amazon.CDK;
using Amazon.CDK.AWS.GuardDuty;
using Amazon.CDK.AWS.SecurityHub;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Constructs;
using System.Collections.Generic;

namespace AwsSapC02Practice.Stacks;

public class SecurityMonitoringStack : Stack
{
    public CfnDetector GuardDutyDetector { get; }
    public CfnHub SecurityHub { get; }
    public ITopic SecurityAlertTopic { get; }

    public SecurityMonitoringStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        SecurityAlertTopic = new Topic(this, "SecurityAlertTopic", new TopicProps
        {
            TopicName = "security-alerts",
            DisplayName = "Security Alerts Topic"
        });

        GuardDutyDetector = new CfnDetector(this, "GuardDutyDetector", new CfnDetectorProps
        {
            Enable = true,
            DataSources = new CfnDetector.CFNDataSourceConfigurationsProperty
            {
                S3Logs = new CfnDetector.CFNS3LogsConfigurationProperty
                {
                    Enable = true
                }
            },
            FindingPublishingFrequency = "FIFTEEN_MINUTES"
        });

        SecurityHub = new CfnHub(this, "SecurityHub", new CfnHubProps
        {
            ControlFindingGenerator = "SECURITY_CONTROL",
            EnableDefaultStandards = true
        });

        var region = Stack.Of(this).Region;

        new CfnStandard(this, "AwsFoundationalStandard", new CfnStandardProps
        {
            StandardsArn = $"arn:aws:securityhub:{region}::standards/aws-foundational-security-best-practices/v/1.0.0"
        });

        new CfnStandard(this, "CisAwsFoundationsBenchmark", new CfnStandardProps
        {
            StandardsArn = $"arn:aws:securityhub:::ruleset/cis-aws-foundations-benchmark/v/1.2.0"
        });

        var guardDutyRule = new Rule(this, "GuardDutyFindingsRule", new RuleProps
        {
            RuleName = "guardduty-findings-rule",
            EventPattern = new EventPattern
            {
                Source = new[] { "aws.guardduty" },
                DetailType = new[] { "GuardDuty Finding" }
            }
        });

        guardDutyRule.AddTarget(new SnsTopic(SecurityAlertTopic));

        new CfnOutput(this, "GuardDutyDetectorId", new CfnOutputProps
        {
            Value = GuardDutyDetector.Ref,
            ExportName = "GuardDutyDetectorId"
        });

        new CfnOutput(this, "SecurityHubArn", new CfnOutputProps
        {
            Value = SecurityHub.AttrArn,
            ExportName = "SecurityHubArn"
        });

        new CfnOutput(this, "SecurityAlertTopicArn", new CfnOutputProps
        {
            Value = SecurityAlertTopic.TopicArn,
            ExportName = "SecurityAlertTopicArn"
        });

        Amazon.CDK.Tags.Of(this).Add("Component", "Security");
        Amazon.CDK.Tags.Of(this).Add("Service", "SecurityMonitoring");
    }
}
