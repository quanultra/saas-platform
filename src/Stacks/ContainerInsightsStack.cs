using Amazon.CDK;
using ECS = Amazon.CDK.AWS.ECS;
using EKS = Amazon.CDK.AWS.EKS;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.CloudWatch;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// Container Insights Stack for ECS and EKS monitoring
    /// Implements Requirement 12.6
    /// </summary>
    public class ContainerInsightsStack : BaseStack
    {
        public LogGroup EcsInsightsLogGroup { get; }
        public LogGroup EksInsightsLogGroup { get; }
        public LogGroup FluentBitLogGroup { get; }
        public Role EcsTaskExecutionRole { get; }
        public Role EksServiceAccountRole { get; }
        public Dashboard ContainerDashboard { get; }

        public ContainerInsightsStack(Construct scope, string id, IStackProps props, StackConfiguration config)
            : base(scope, id, props, config)
        {
            // Create log groups for Container Insights
            EcsInsightsLogGroup = new LogGroup(this, "EcsInsightsLogGroup", new LogGroupProps
            {
                LogGroupName = $"/aws/containerinsights/{GenerateResourceName("cluster")}/performance",
                Retention = RetentionDays.ONE_MONTH,
                RemovalPolicy = config.Environment == "prod" ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
            });

            FluentBitLogGroup = new LogGroup(this, "FluentBitLogGroup", new LogGroupProps
            {
                LogGroupName = $"/aws/containerinsights/{GenerateResourceName("cluster")}/application",
                Retention = RetentionDays.ONE_MONTH,
                RemovalPolicy = config.Environment == "prod" ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
            });

            // Create IAM role for ECS task execution with Container Insights permissions
            EcsTaskExecutionRole = new Role(this, "EcsTaskExecutionRole", new RoleProps
            {
                RoleName = GenerateResourceName("ecs-task-execution-role"),
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy"),
                    ManagedPolicy.FromAwsManagedPolicyName("CloudWatchAgentServerPolicy")
                }
            });

            // Add inline policy for Container Insights
            EcsTaskExecutionRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[]
                {
                    "logs:CreateLogGroup",
                    "logs:CreateLogStream",
                    "logs:PutLogEvents",
                    "logs:DescribeLogStreams",
                    "cloudwatch:PutMetricData",
                    "ec2:DescribeVolumes",
                    "ec2:DescribeTags",
                    "ecs:DescribeTasks",
                    "ecs:DescribeContainerInstances",
                    "ecs:DescribeTaskDefinition"
                },
                Resources = new[] { "*" }
            }));

            // Create IAM role for EKS service account with Container Insights permissions
            EksServiceAccountRole = new Role(this, "EksServiceAccountRole", new RoleProps
            {
                RoleName = GenerateResourceName("eks-container-insights-role"),
                AssumedBy = new ServicePrincipal("eks.amazonaws.com"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("CloudWatchAgentServerPolicy")
                }
            });

            // Add inline policy for EKS Container Insights
            EksServiceAccountRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[]
                {
                    "logs:CreateLogGroup",
                    "logs:CreateLogStream",
                    "logs:PutLogEvents",
                    "logs:DescribeLogStreams",
                    "cloudwatch:PutMetricData",
                    "ec2:DescribeVolumes",
                    "ec2:DescribeTags",
                    "eks:DescribeCluster"
                },
                Resources = new[] { "*" }
            }));

            // Create Container Insights dashboard
            ContainerDashboard = new Dashboard(this, "ContainerDashboard", new DashboardProps
            {
                DashboardName = GenerateResourceName("container-insights-dashboard")
            });

            ConfigureContainerDashboard();

            // Create Container Insights alarms
            CreateContainerInsightsAlarms();

            // Create outputs
            CreateOutput("EcsInsightsLogGroupName", EcsInsightsLogGroup.LogGroupName, "ECS Container Insights log group");
            CreateOutput("EksInsightsLogGroupName", EksInsightsLogGroup.LogGroupName, "EKS Container Insights log group");
            CreateOutput("FluentBitLogGroupName", FluentBitLogGroup.LogGroupName, "Fluent Bit log group");
            CreateOutput("EcsTaskExecutionRoleArn", EcsTaskExecutionRole.RoleArn, "ECS task execution role ARN");
            CreateOutput("EksServiceAccountRoleArn", EksServiceAccountRole.RoleArn, "EKS service account role ARN");
            CreateOutput("ContainerDashboardUrl", $"https://console.aws.amazon.com/cloudwatch/home?region={this.Region}#dashboards:name={ContainerDashboard.DashboardName}", "Container Insights dashboard URL");
        }

        private void ConfigureContainerDashboard()
        {
            // ECS CPU utilization widget
            ContainerDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "ECS Cluster CPU Utilization",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "ECS/ContainerInsights",
                        MetricName = "CpuUtilized",
                        DimensionsMap = new Dictionary<string, string>
                        {
                            ["ClusterName"] = GenerateResourceName("cluster")
                        },
                        Statistic = "Average",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));

            // ECS Memory utilization widget
            ContainerDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "ECS Cluster Memory Utilization",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "ECS/ContainerInsights",
                        MetricName = "MemoryUtilized",
                        DimensionsMap = new Dictionary<string, string>
                        {
                            ["ClusterName"] = GenerateResourceName("cluster")
                        },
                        Statistic = "Average",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));

            // ECS Network metrics widget
            ContainerDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "ECS Network Traffic",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "ECS/ContainerInsights",
                        MetricName = "NetworkRxBytes",
                        DimensionsMap = new Dictionary<string, string>
                        {
                            ["ClusterName"] = GenerateResourceName("cluster")
                        },
                        Statistic = "Sum",
                        Period = Duration.Minutes(5)
                    }),
                    new Metric(new MetricProps
                    {
                        Namespace = "ECS/ContainerInsights",
                        MetricName = "NetworkTxBytes",
                        DimensionsMap = new Dictionary<string, string>
                        {
                            ["ClusterName"] = GenerateResourceName("cluster")
                        },
                        Statistic = "Sum",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));

            // ECS Task count widget
            ContainerDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "ECS Running Tasks",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "ECS/ContainerInsights",
                        MetricName = "RunningTaskCount",
                        DimensionsMap = new Dictionary<string, string>
                        {
                            ["ClusterName"] = GenerateResourceName("cluster")
                        },
                        Statistic = "Average",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));

            // EKS Node CPU utilization widget
            ContainerDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "EKS Node CPU Utilization",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "ContainerInsights",
                        MetricName = "node_cpu_utilization",
                        DimensionsMap = new Dictionary<string, string>
                        {
                            ["ClusterName"] = GenerateResourceName("cluster")
                        },
                        Statistic = "Average",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));

            // EKS Node Memory utilization widget
            ContainerDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "EKS Node Memory Utilization",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "ContainerInsights",
                        MetricName = "node_memory_utilization",
                        DimensionsMap = new Dictionary<string, string>
                        {
                            ["ClusterName"] = GenerateResourceName("cluster")
                        },
                        Statistic = "Average",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));

            // EKS Pod count widget
            ContainerDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "EKS Pod Count",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "ContainerInsights",
                        MetricName = "pod_number_of_containers",
                        DimensionsMap = new Dictionary<string, string>
                        {
                            ["ClusterName"] = GenerateResourceName("cluster")
                        },
                        Statistic = "Sum",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));

            // EKS Network metrics widget
            ContainerDashboard.AddWidgets(new GraphWidget(new GraphWidgetProps
            {
                Title = "EKS Network Traffic",
                Left = new[]
                {
                    new Metric(new MetricProps
                    {
                        Namespace = "ContainerInsights",
                        MetricName = "pod_network_rx_bytes",
                        DimensionsMap = new Dictionary<string, string>
                        {
                            ["ClusterName"] = GenerateResourceName("cluster")
                        },
                        Statistic = "Sum",
                        Period = Duration.Minutes(5)
                    }),
                    new Metric(new MetricProps
                    {
                        Namespace = "ContainerInsights",
                        MetricName = "pod_network_tx_bytes",
                        DimensionsMap = new Dictionary<string, string>
                        {
                            ["ClusterName"] = GenerateResourceName("cluster")
                        },
                        Statistic = "Sum",
                        Period = Duration.Minutes(5)
                    })
                },
                Width = 12,
                Height = 6
            }));
        }

        private void CreateContainerInsightsAlarms()
        {
            // ECS high CPU alarm
            new Alarm(this, "EcsHighCpuAlarm", new AlarmProps
            {
                AlarmName = GenerateResourceName("ecs-high-cpu"),
                AlarmDescription = "ECS cluster CPU utilization is too high",
                Metric = new Metric(new MetricProps
                {
                    Namespace = "ECS/ContainerInsights",
                    MetricName = "CpuUtilized",
                    DimensionsMap = new Dictionary<string, string>
                    {
                        ["ClusterName"] = GenerateResourceName("cluster")
                    },
                    Statistic = "Average",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 80,
                EvaluationPeriods = 2,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD
            });

            // ECS high memory alarm
            new Alarm(this, "EcsHighMemoryAlarm", new AlarmProps
            {
                AlarmName = GenerateResourceName("ecs-high-memory"),
                AlarmDescription = "ECS cluster memory utilization is too high",
                Metric = new Metric(new MetricProps
                {
                    Namespace = "ECS/ContainerInsights",
                    MetricName = "MemoryUtilized",
                    DimensionsMap = new Dictionary<string, string>
                    {
                        ["ClusterName"] = GenerateResourceName("cluster")
                    },
                    Statistic = "Average",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 85,
                EvaluationPeriods = 2,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD
            });

            // EKS high CPU alarm
            new Alarm(this, "EksHighCpuAlarm", new AlarmProps
            {
                AlarmName = GenerateResourceName("eks-high-cpu"),
                AlarmDescription = "EKS node CPU utilization is too high",
                Metric = new Metric(new MetricProps
                {
                    Namespace = "ContainerInsights",
                    MetricName = "node_cpu_utilization",
                    DimensionsMap = new Dictionary<string, string>
                    {
                        ["ClusterName"] = GenerateResourceName("cluster")
                    },
                    Statistic = "Average",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 80,
                EvaluationPeriods = 2,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD
            });

            // EKS high memory alarm
            new Alarm(this, "EksHighMemoryAlarm", new AlarmProps
            {
                AlarmName = GenerateResourceName("eks-high-memory"),
                AlarmDescription = "EKS node memory utilization is too high",
                Metric = new Metric(new MetricProps
                {
                    Namespace = "ContainerInsights",
                    MetricName = "node_memory_utilization",
                    DimensionsMap = new Dictionary<string, string>
                    {
                        ["ClusterName"] = GenerateResourceName("cluster")
                    },
                    Statistic = "Average",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 85,
                EvaluationPeriods = 2,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD
            });
        }

        /// <summary>
        /// Helper method to enable Container Insights on an ECS cluster
        /// </summary>
        public void EnableContainerInsightsForEcs(ECS.Cluster cluster)
        {
            // Container Insights is enabled via cluster configuration
            // This method is a placeholder for additional ECS-specific Container Insights configuration
        }

        /// <summary>
        /// Get Fluent Bit configuration for EKS
        /// </summary>
        public string GetFluentBitConfigMap()
        {
            return @"
apiVersion: v1
kind: ConfigMap
metadata:
  name: fluent-bit-config
  namespace: amazon-cloudwatch
data:
  fluent-bit.conf: |
    [SERVICE]
        Flush                     5
        Log_Level                 info
        Daemon                    off
        Parsers_File              parsers.conf
        HTTP_Server               On
        HTTP_Listen               0.0.0.0
        HTTP_Port                 2020

    [INPUT]
        Name                tail
        Path                /var/log/containers/*.log
        Parser              docker
        Tag                 kube.*
        Refresh_Interval    5
        Mem_Buf_Limit       5MB
        Skip_Long_Lines     On

    [FILTER]
        Name                kubernetes
        Match               kube.*
        Kube_URL            https://kubernetes.default.svc:443
        Kube_CA_File        /var/run/secrets/kubernetes.io/serviceaccount/ca.crt
        Kube_Token_File     /var/run/secrets/kubernetes.io/serviceaccount/token
        Merge_Log           On
        Keep_Log            Off

    [OUTPUT]
        Name                cloudwatch_logs
        Match               *
        region              " + this.Region + @"
        log_group_name      " + FluentBitLogGroup.LogGroupName + @"
        log_stream_prefix   fluent-bit-
        auto_create_group   true
";
        }
    }
}
