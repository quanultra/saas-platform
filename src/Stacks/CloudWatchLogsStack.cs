using Amazon.CDK;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.IAM;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// CloudWatch Logs Stack with log groups, retention, insights queries, and metric filters
    /// Implements Requirements 12.2, 12.4
    /// </summary>
    public class CloudWatchLogsStack : BaseStack
    {
        public LogGroup ApplicationLogGroup { get; }
        public LogGroup InfrastructureLogGroup { get; }
        public LogGroup SecurityLogGroup { get; }
        public LogGroup AuditLogGroup { get; }
        public QueryDefinition InsightsQuery { get; }
        public QueryDefinition ErrorQueryDefinition { get; }
        public QueryDefinition PerformanceQueryDefinition { get; }
        public QueryDefinition SecurityQueryDefinition { get; }

        public CloudWatchLogsStack(Construct scope, string id, StackConfiguration config, IStackProps props = null)
            : base(scope, id, props, config)
        {
            // Determine retention based on environment
            var retentionDays = config.Environment == "prod" ? RetentionDays.ONE_MONTH : RetentionDays.ONE_WEEK;
            var securityRetentionDays = RetentionDays.ONE_YEAR; // Security logs always retained for 1 year (365 days minimum for compliance)

            // Create application log group
            ApplicationLogGroup = new LogGroup(this, "ApplicationLogGroup", new LogGroupProps
            {
                LogGroupName = $"/aws/{GenerateResourceName("application")}",
                Retention = retentionDays,
                RemovalPolicy = config.Environment == "prod" ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
            });

            // Create infrastructure log group
            InfrastructureLogGroup = new LogGroup(this, "InfrastructureLogGroup", new LogGroupProps
            {
                LogGroupName = $"/aws/{GenerateResourceName("infrastructure")}",
                Retention = retentionDays,
                RemovalPolicy = config.Environment == "prod" ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
            });

            // Create security log group
            SecurityLogGroup = new LogGroup(this, "SecurityLogGroup", new LogGroupProps
            {
                LogGroupName = $"/aws/{GenerateResourceName("security")}",
                Retention = RetentionDays.ONE_YEAR, // Security logs retained longer
                RemovalPolicy = RemovalPolicy.RETAIN // Always retain security logs
            });

            // Create audit log group
            AuditLogGroup = new LogGroup(this, "AuditLogGroup", new LogGroupProps
            {
                LogGroupName = $"/aws/{GenerateResourceName("audit")}",
                Retention = RetentionDays.ONE_YEAR, // Audit logs retained longer
                RemovalPolicy = RemovalPolicy.RETAIN // Always retain audit logs
            });

            // Configure metric filters for application logs
            ConfigureApplicationMetricFilters();

            // Configure metric filters for infrastructure logs
            ConfigureInfrastructureMetricFilters();

            // Configure metric filters for security logs
            ConfigureSecurityMetricFilters();

            // Create Log Insights query definitions
            ErrorQueryDefinition = new QueryDefinition(this, "ErrorQueryDefinition", new QueryDefinitionProps
            {
                QueryDefinitionName = GenerateResourceName("error-analysis"),
                QueryString = new QueryString(new QueryStringProps
                {
                    Fields = new[] { "@timestamp", "@message", "level", "error", "stack" },
                    FilterStatements = new[] { "level = \"ERROR\" or level = \"FATAL\"" },
                    Sort = "@timestamp desc"
                }),
                LogGroups = new[] { ApplicationLogGroup }
            });

            PerformanceQueryDefinition = new QueryDefinition(this, "PerformanceQueryDefinition", new QueryDefinitionProps
            {
                QueryDefinitionName = GenerateResourceName("performance-analysis"),
                QueryString = new QueryString(new QueryStringProps
                {
                    Fields = new[] { "@timestamp", "requestId", "duration", "statusCode", "path" },
                    FilterStatements = new[] { "duration > 1000" },
                    Sort = "duration desc"
                }),
                LogGroups = new[] { ApplicationLogGroup }
            });

            SecurityQueryDefinition = new QueryDefinition(this, "SecurityQueryDefinition", new QueryDefinitionProps
            {
                QueryDefinitionName = GenerateResourceName("security-events"),
                QueryString = new QueryString(new QueryStringProps
                {
                    Fields = new[] { "@timestamp", "eventType", "sourceIP", "userAgent", "action", "result" },
                    FilterStatements = new[] { "eventType = \"authentication\" or eventType = \"authorization\"" },
                    Sort = "@timestamp desc"
                }),
                LogGroups = new[] { SecurityLogGroup }
            });

            // Create additional query definitions
            CreateAdvancedQueryDefinitions();

            // Create outputs
            CreateOutput("ApplicationLogGroupName", ApplicationLogGroup.LogGroupName, "Application log group name");
            CreateOutput("ApplicationLogGroupArn", ApplicationLogGroup.LogGroupArn, "Application log group ARN");
            CreateOutput("InfrastructureLogGroupName", InfrastructureLogGroup.LogGroupName, "Infrastructure log group name");
            CreateOutput("SecurityLogGroupName", SecurityLogGroup.LogGroupName, "Security log group name");
            CreateOutput("AuditLogGroupName", AuditLogGroup.LogGroupName, "Audit log group name");
        }

        private void ConfigureApplicationMetricFilters()
        {
            // Error count metric filter
            new MetricFilter(this, "ErrorCountMetricFilter", new MetricFilterProps
            {
                LogGroup = ApplicationLogGroup,
                MetricNamespace = "CustomApp/Errors",
                MetricName = "ErrorCount",
                FilterPattern = FilterPattern.Literal("[time, request_id, level = ERROR*, ...]"),
                MetricValue = "1",
                DefaultValue = 0
            });

            // Warning count metric filter
            new MetricFilter(this, "WarningCountMetricFilter", new MetricFilterProps
            {
                LogGroup = ApplicationLogGroup,
                MetricNamespace = "CustomApp/Warnings",
                MetricName = "WarningCount",
                FilterPattern = FilterPattern.Literal("[time, request_id, level = WARN*, ...]"),
                MetricValue = "1",
                DefaultValue = 0
            });

            // Response time metric filter
            new MetricFilter(this, "ResponseTimeMetricFilter", new MetricFilterProps
            {
                LogGroup = ApplicationLogGroup,
                MetricNamespace = "CustomApp/Performance",
                MetricName = "ResponseTime",
                FilterPattern = FilterPattern.Literal("[..., duration, ...]"),
                MetricValue = "$duration",
                DefaultValue = 0,
                Unit = Unit.MILLISECONDS
            });

            // 4XX error metric filter
            new MetricFilter(this, "ClientErrorMetricFilter", new MetricFilterProps
            {
                LogGroup = ApplicationLogGroup,
                MetricNamespace = "CustomApp/HTTP",
                MetricName = "4XXErrors",
                FilterPattern = FilterPattern.Literal("[..., status_code = 4*, ...]"),
                MetricValue = "1",
                DefaultValue = 0
            });

            // 5XX error metric filter
            new MetricFilter(this, "ServerErrorMetricFilter", new MetricFilterProps
            {
                LogGroup = ApplicationLogGroup,
                MetricNamespace = "CustomApp/HTTP",
                MetricName = "5XXErrors",
                FilterPattern = FilterPattern.Literal("[..., status_code = 5*, ...]"),
                MetricValue = "1",
                DefaultValue = 0
            });
        }

        private void ConfigureInfrastructureMetricFilters()
        {
            // Database connection errors
            new MetricFilter(this, "DatabaseConnectionErrorMetricFilter", new MetricFilterProps
            {
                LogGroup = InfrastructureLogGroup,
                MetricNamespace = "CustomApp/Database",
                MetricName = "ConnectionErrors",
                FilterPattern = FilterPattern.AnyTerm("connection error", "connection timeout", "connection refused"),
                MetricValue = "1",
                DefaultValue = 0
            });

            // Memory usage metric filter
            new MetricFilter(this, "HighMemoryUsageMetricFilter", new MetricFilterProps
            {
                LogGroup = InfrastructureLogGroup,
                MetricNamespace = "CustomApp/Resources",
                MetricName = "HighMemoryUsage",
                FilterPattern = FilterPattern.AnyTerm("OutOfMemoryError", "memory exceeded"),
                MetricValue = "1",
                DefaultValue = 0
            });

            // Disk space warnings
            new MetricFilter(this, "DiskSpaceWarningMetricFilter", new MetricFilterProps
            {
                LogGroup = InfrastructureLogGroup,
                MetricNamespace = "CustomApp/Resources",
                MetricName = "DiskSpaceWarnings",
                FilterPattern = FilterPattern.AnyTerm("disk space low", "disk full"),
                MetricValue = "1",
                DefaultValue = 0
            });
        }

        private void ConfigureSecurityMetricFilters()
        {
            // Failed authentication attempts
            new MetricFilter(this, "FailedAuthMetricFilter", new MetricFilterProps
            {
                LogGroup = SecurityLogGroup,
                MetricNamespace = "CustomApp/Security",
                MetricName = "FailedAuthentications",
                FilterPattern = FilterPattern.AnyTerm("authentication failed", "login failed", "invalid credentials"),
                MetricValue = "1",
                DefaultValue = 0
            });

            // Unauthorized access attempts
            new MetricFilter(this, "UnauthorizedAccessMetricFilter", new MetricFilterProps
            {
                LogGroup = SecurityLogGroup,
                MetricNamespace = "CustomApp/Security",
                MetricName = "UnauthorizedAccess",
                FilterPattern = FilterPattern.AnyTerm("unauthorized", "access denied", "forbidden"),
                MetricValue = "1",
                DefaultValue = 0
            });

            // Suspicious activity
            new MetricFilter(this, "SuspiciousActivityMetricFilter", new MetricFilterProps
            {
                LogGroup = SecurityLogGroup,
                MetricNamespace = "CustomApp/Security",
                MetricName = "SuspiciousActivity",
                FilterPattern = FilterPattern.AnyTerm("sql injection", "xss attack", "brute force"),
                MetricValue = "1",
                DefaultValue = 0
            });
        }

        private void CreateAdvancedQueryDefinitions()
        {
            // Top errors query
            new QueryDefinition(this, "TopErrorsQuery", new QueryDefinitionProps
            {
                QueryDefinitionName = GenerateResourceName("top-errors"),
                QueryString = new QueryString(new QueryStringProps
                {
                    Fields = new[] { "error" },
                    FilterStatements = new[] { "level = \"ERROR\"" },
                    Stats = "count(*) as errorCount by error",
                    Sort = "errorCount desc",
                    Limit = 10
                }),
                LogGroups = new[] { ApplicationLogGroup }
            });

            // User activity query
            new QueryDefinition(this, "UserActivityQuery", new QueryDefinitionProps
            {
                QueryDefinitionName = GenerateResourceName("user-activity"),
                QueryString = new QueryString(new QueryStringProps
                {
                    Fields = new[] { "@timestamp", "userId", "action", "resource" },
                    FilterStatements = new[] { "userId exists" },
                    Sort = "@timestamp desc"
                }),
                LogGroups = new[] { AuditLogGroup }
            });

            // API latency percentiles query
            new QueryDefinition(this, "ApiLatencyPercentilesQuery", new QueryDefinitionProps
            {
                QueryDefinitionName = GenerateResourceName("api-latency-percentiles"),
                QueryString = new QueryString(new QueryStringProps
                {
                    Fields = new[] { "duration" },
                    FilterStatements = new[] { "duration exists" },
                    Stats = "pct(duration, 50) as p50, pct(duration, 90) as p90, pct(duration, 99) as p99"
                }),
                LogGroups = new[] { ApplicationLogGroup }
            });

            // Failed requests by endpoint query
            new QueryDefinition(this, "FailedRequestsByEndpointQuery", new QueryDefinitionProps
            {
                QueryDefinitionName = GenerateResourceName("failed-requests-by-endpoint"),
                QueryString = new QueryString(new QueryStringProps
                {
                    Fields = new[] { "path", "statusCode" },
                    FilterStatements = new[] { "statusCode >= 400" },
                    Stats = "count(*) as failureCount by path",
                    Sort = "failureCount desc"
                }),
                LogGroups = new[] { ApplicationLogGroup }
            });
        }

        private RetentionDays GetRetentionDays(int days)
        {
            return days switch
            {
                1 => RetentionDays.ONE_DAY,
                3 => RetentionDays.THREE_DAYS,
                5 => RetentionDays.FIVE_DAYS,
                7 => RetentionDays.ONE_WEEK,
                14 => RetentionDays.TWO_WEEKS,
                30 => RetentionDays.ONE_MONTH,
                60 => RetentionDays.TWO_MONTHS,
                90 => RetentionDays.THREE_MONTHS,
                120 => RetentionDays.FOUR_MONTHS,
                150 => RetentionDays.FIVE_MONTHS,
                180 => RetentionDays.SIX_MONTHS,
                365 => RetentionDays.ONE_YEAR,
                _ => RetentionDays.ONE_MONTH
            };
        }
    }
}
