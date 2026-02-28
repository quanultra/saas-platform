using Amazon.CDK;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.CloudWatch.Actions;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// CloudWatch Monitoring Stack with custom dashboards, metrics, and alarms
    /// Implements Requirements 12.1, 12.3
    /// </summary>
    public class MonitoringStack : BaseStack
    {
        public Dashboard MainDashboard { get; }
        public Dashboard InfrastructureDashboard { get; }
        public Dashboard ApplicationDashboard { get; }
        public Topic AlarmTopic { get; }
        public CompositeAlarm CriticalCompositeAlarm { get; }

        public MonitoringStack(Construct scope, string id, StackConfiguration config, IStackProps props = null)
            : base(scope, id, props, config)
        {
            // Create SNS topic for alarms
            AlarmTopic = new Topic(this, "AlarmTopic", new TopicProps
            {
                TopicName = GenerateResourceName("monitoring-alarms"),
                DisplayName = "CloudWatch Alarms Notification Topic"
            });

            // Add email subscription (configure email in config)
            if (!string.IsNullOrEmpty(config.Monitoring?.AlarmEmail))
            {
                AlarmTopic.AddSubscription(new EmailSubscription(config.Monitoring.AlarmEmail));
            }

            // Create main dashboard
            MainDashboard = new Dashboard(this, "MainDashboard", new DashboardProps
            {
                DashboardName = GenerateResourceName("main-dashboard")
            });

            // Create infrastructure dashboard
            InfrastructureDashboard = new Dashboard(this, "InfrastructureDashboard", new DashboardProps
            {
                DashboardName = GenerateResourceName("infrastructure-dashboard")
            });

            // Create application dashboard
            ApplicationDashboard = new Dashboard(this, "ApplicationDashboard", new DashboardProps
            {
                DashboardName = GenerateResourceName("application-dashboard")
            });

            // Configure main dashboard widgets
            ConfigureMainDashboard();

            // Configure infrastructure dashboard
            ConfigureInfrastructureDashboard();

            // Configure application dashboard
            ConfigureApplicationDashboard();

            // Create individual alarms
            var cpuAlarm = CreateCpuAlarm();
            var memoryAlarm = CreateMemoryAlarm();
            var diskAlarm = CreateDiskAlarm();
            var apiErrorAlarm = CreateApiErrorAlarm();

            // Create composite alarm
            CriticalCompositeAlarm = new CompositeAlarm(this, "CriticalCompositeAlarm", new CompositeAlarmProps
            {
                CompositeAlarmName = GenerateResourceName("critical-composite-alarm"),
                AlarmDescription = "Composite alarm for critical infrastructure issues",
                AlarmRule = AlarmRule.AnyOf(
                    AlarmRule.FromAlarm(cpuAlarm, AlarmState.ALARM),
                    AlarmRule.FromAlarm(memoryAlarm, AlarmState.ALARM),
                    AlarmRule.AllOf(
                        AlarmRule.FromAlarm(diskAlarm, AlarmState.ALARM),
                        AlarmRule.FromAlarm(apiErrorAlarm, AlarmState.ALARM)
                    )
                ),
                ActionsEnabled = true
            });

            CriticalCompositeAlarm.AddAlarmAction(new SnsAction(AlarmTopic));

            // Create outputs
            CreateOutput("MainDashboardUrl", $"https://console.aws.amazon.com/cloudwatch/home?region={this.Region}#dashboards:name={MainDashboard.DashboardName}", "Main dashboard URL");
            CreateOutput("InfrastructureDashboardUrl", $"https://console.aws.amazon.com/cloudwatch/home?region={this.Region}#dashboards:name={InfrastructureDashboard.DashboardName}", "Infrastructure dashboard URL");
            CreateOutput("ApplicationDashboardUrl", $"https://console.aws.amazon.com/cloudwatch/home?region={this.Region}#dashboards:name={ApplicationDashboard.DashboardName}", "Application dashboard URL");
            CreateOutput("AlarmTopicArn", AlarmTopic.TopicArn, "Alarm notification topic ARN");
            CreateOutput("CriticalCompositeAlarmArn", CriticalCompositeAlarm.AlarmArn, "Critical composite alarm ARN");
        }

        private void ConfigureMainDashboard()
        {
            // Add EC2 metrics widget
            MainDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "EC2 CPU Utilization",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/EC2",
                        MetricName = "CPUUtilization",
                        Statistic = "Average",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));

            // Add RDS metrics widget
            MainDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "RDS Database Connections",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/RDS",
                        MetricName = "DatabaseConnections",
                        Statistic = "Average",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));

            // Add Lambda metrics widget
            MainDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "Lambda Invocations & Errors",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/Lambda",
                        MetricName = "Invocations",
                        Statistic = "Sum",
                        Period = Duration.Minutes(5)
                    })
                },
                Right = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/Lambda",
                        MetricName = "Errors",
                        Statistic = "Sum",
                        Period = Duration.Minutes(5),
                        Color = "#d62728"
                    })
                },
                Width = 12,
                Height = 6
            }));

            // Add API Gateway metrics widget
            MainDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "API Gateway Requests",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/ApiGateway",
                        MetricName = "Count",
                        Statistic = "Sum",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));
        }

        private void ConfigureInfrastructureDashboard()
        {
            // Add VPC metrics
            InfrastructureDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "VPC Network Traffic",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/EC2",
                        MetricName = "NetworkIn",
                        Statistic = "Sum",
                        Period = Duration.Minutes(5)
                    }),
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/EC2",
                        MetricName = "NetworkOut",
                        Statistic = "Sum",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));

            // Add ELB metrics
            InfrastructureDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "Load Balancer Health",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/ApplicationELB",
                        MetricName = "HealthyHostCount",
                        Statistic = "Average",
                        Period = Duration.Minutes(5)
                    }),
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/ApplicationELB",
                        MetricName = "UnHealthyHostCount",
                        Statistic = "Average",
                        Period = Duration.Minutes(5),
                        Color = "#d62728"
                    })
                },
                Width = 12,
                Height = 6
            }));

            // Add Auto Scaling metrics
            InfrastructureDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "Auto Scaling Group Size",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/AutoScaling",
                        MetricName = "GroupDesiredCapacity",
                        Statistic = "Average",
                        Period = Duration.Minutes(5)
                    }),
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/AutoScaling",
                        MetricName = "GroupInServiceInstances",
                        Statistic = "Average",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));
        }

        private void ConfigureApplicationDashboard()
        {
            // Add application-specific metrics
            ApplicationDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "Application Response Time",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/ApplicationELB",
                        MetricName = "TargetResponseTime",
                        Statistic = "Average",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));

            // Add error rate metrics
            ApplicationDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "Application Error Rate",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/ApplicationELB",
                        MetricName = "HTTPCode_Target_4XX_Count",
                        Statistic = "Sum",
                        Period = Duration.Minutes(5),
                        Color = "#ff7f0e"
                    }),
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/ApplicationELB",
                        MetricName = "HTTPCode_Target_5XX_Count",
                        Statistic = "Sum",
                        Period = Duration.Minutes(5),
                        Color = "#d62728"
                    })
                },
                Width = 12,
                Height = 6
            }));

            // Add throughput metrics
            ApplicationDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "Application Throughput",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "AWS/ApplicationELB",
                        MetricName = "RequestCount",
                        Statistic = "Sum",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));
        }

        private Alarm CreateCpuAlarm()
        {
            var alarm = new Alarm(this, "HighCpuAlarm", new AlarmProps
            {
                AlarmName = GenerateResourceName("high-cpu-alarm"),
                AlarmDescription = "Alert when CPU utilization is too high",
                Metric = new Metric(new MetricProps
                {
                    Namespace = "AWS/EC2",
                    MetricName = "CPUUtilization",
                    Statistic = "Average",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 80,
                EvaluationPeriods = 2,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
                ActionsEnabled = true
            });

            alarm.AddAlarmAction(new SnsAction(AlarmTopic));
            return alarm;
        }

        private Alarm CreateMemoryAlarm()
        {
            var alarm = new Alarm(this, "HighMemoryAlarm", new AlarmProps
            {
                AlarmName = GenerateResourceName("high-memory-alarm"),
                AlarmDescription = "Alert when memory utilization is too high",
                Metric = new Metric(new MetricProps
                {
                    Namespace = "CWAgent",
                    MetricName = "mem_used_percent",
                    Statistic = "Average",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 85,
                EvaluationPeriods = 2,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
                ActionsEnabled = true
            });

            alarm.AddAlarmAction(new SnsAction(AlarmTopic));
            return alarm;
        }

        private Alarm CreateDiskAlarm()
        {
            var alarm = new Alarm(this, "HighDiskUsageAlarm", new AlarmProps
            {
                AlarmName = GenerateResourceName("high-disk-usage-alarm"),
                AlarmDescription = "Alert when disk usage is too high",
                Metric = new Metric(new MetricProps
                {
                    Namespace = "CWAgent",
                    MetricName = "disk_used_percent",
                    Statistic = "Average",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 90,
                EvaluationPeriods = 2,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
                ActionsEnabled = true
            });

            alarm.AddAlarmAction(new SnsAction(AlarmTopic));
            return alarm;
        }

        private Alarm CreateApiErrorAlarm()
        {
            var alarm = new Alarm(this, "HighApiErrorRateAlarm", new AlarmProps
            {
                AlarmName = GenerateResourceName("high-api-error-rate-alarm"),
                AlarmDescription = "Alert when API error rate is too high",
                Metric = new Metric(new MetricProps
                {
                    Namespace = "AWS/ApiGateway",
                    MetricName = "5XXError",
                    Statistic = "Sum",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 10,
                EvaluationPeriods = 2,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
                ActionsEnabled = true
            });

            alarm.AddAlarmAction(new SnsAction(AlarmTopic));
            return alarm;
        }
    }
}
