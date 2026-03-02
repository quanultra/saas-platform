using System;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Models
{
    public class EnvironmentConfig
    {
        public string EnvironmentName { get; set; }
        public string AwsAccount { get; set; }
        public string PrimaryRegion { get; set; }
        public string SecondaryRegion { get; set; }
        public Dictionary<string, string> ParameterStoreValues { get; set; } = new();
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        public ResourceSizingConfig ResourceSizing { get; set; } = new();
        public CostConfig CostSettings { get; set; } = new();

        public static EnvironmentConfig GetDevConfig()
        {
            return new EnvironmentConfig
            {
                EnvironmentName = "dev",
                AwsAccount = Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT") ?? "",
                PrimaryRegion = "us-east-1",
                SecondaryRegion = "us-west-2",
                ParameterStoreValues = new Dictionary<string, string>
                {
                    ["/aws-sap-c02/dev/db-name"] = "sapc02dev",
                    ["/aws-sap-c02/dev/alarm-email"] = "dev-alerts@example.com",
                    ["/aws-sap-c02/dev/log-retention-days"] = "7"
                },
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["ENVIRONMENT"] = "dev",
                    ["LOG_LEVEL"] = "DEBUG",
                    ["ENABLE_XRAY"] = "true"
                },
                ResourceSizing = new ResourceSizingConfig
                {
                    RdsInstanceClass = "db.t3.medium",
                    EcsTaskCpu = 256,
                    EcsTaskMemory = 512,
                    EksNodeInstanceType = "t3.medium",
                    EksMinNodes = 1,
                    EksMaxNodes = 3,
                    ElastiCacheNodeType = "cache.t3.micro",
                    EnableMultiAz = false
                },
                CostSettings = new CostConfig
                {
                    MonthlyBudget = 100,
                    EnableSpotInstances = true,
                    EnableAutoShutdown = true,
                    ShutdownSchedule = "0 20 * * *"
                }
            };
        }

        public static EnvironmentConfig GetStagingConfig()
        {
            return new EnvironmentConfig
            {
                EnvironmentName = "staging",
                AwsAccount = Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT") ?? "",
                PrimaryRegion = "us-east-1",
                SecondaryRegion = "eu-west-1",
                ParameterStoreValues = new Dictionary<string, string>
                {
                    ["/aws-sap-c02/staging/db-name"] = "sapc02staging",
                    ["/aws-sap-c02/staging/alarm-email"] = "staging-alerts@example.com",
                    ["/aws-sap-c02/staging/log-retention-days"] = "30"
                },
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["ENVIRONMENT"] = "staging",
                    ["LOG_LEVEL"] = "INFO",
                    ["ENABLE_XRAY"] = "true"
                },
                ResourceSizing = new ResourceSizingConfig
                {
                    RdsInstanceClass = "db.r5.large",
                    EcsTaskCpu = 512,
                    EcsTaskMemory = 1024,
                    EksNodeInstanceType = "t3.large",
                    EksMinNodes = 2,
                    EksMaxNodes = 5,
                    ElastiCacheNodeType = "cache.t3.small",
                    EnableMultiAz = true
                },
                CostSettings = new CostConfig
                {
                    MonthlyBudget = 500,
                    EnableSpotInstances = true,
                    EnableAutoShutdown = false
                }
            };
        }

        public static EnvironmentConfig GetProdConfig()
        {
            return new EnvironmentConfig
            {
                EnvironmentName = "prod",
                AwsAccount = Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT") ?? "",
                PrimaryRegion = "us-east-1",
                SecondaryRegion = "eu-west-1",
                ParameterStoreValues = new Dictionary<string, string>
                {
                    ["/aws-sap-c02/prod/db-name"] = "sapc02prod",
                    ["/aws-sap-c02/prod/alarm-email"] = "prod-alerts@example.com",
                    ["/aws-sap-c02/prod/log-retention-days"] = "90"
                },
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["ENVIRONMENT"] = "prod",
                    ["LOG_LEVEL"] = "WARN",
                    ["ENABLE_XRAY"] = "true"
                },
                ResourceSizing = new ResourceSizingConfig
                {
                    RdsInstanceClass = "db.r5.xlarge",
                    EcsTaskCpu = 1024,
                    EcsTaskMemory = 2048,
                    EksNodeInstanceType = "m5.xlarge",
                    EksMinNodes = 3,
                    EksMaxNodes = 10,
                    ElastiCacheNodeType = "cache.r5.large",
                    EnableMultiAz = true
                },
                CostSettings = new CostConfig
                {
                    MonthlyBudget = 2000,
                    EnableSpotInstances = false,
                    EnableAutoShutdown = false
                }
            };
        }

        public static EnvironmentConfig GetConfig(string environmentName)
        {
            return environmentName?.ToLower() switch
            {
                "dev" => GetDevConfig(),
                "staging" => GetStagingConfig(),
                "prod" => GetProdConfig(),
                _ => GetDevConfig()
            };
        }
    }

    public class ResourceSizingConfig
    {
        public string RdsInstanceClass { get; set; }
        public int EcsTaskCpu { get; set; }
        public int EcsTaskMemory { get; set; }
        public string EksNodeInstanceType { get; set; }
        public int EksMinNodes { get; set; }
        public int EksMaxNodes { get; set; }
        public string ElastiCacheNodeType { get; set; }
        public bool EnableMultiAz { get; set; }
    }

    public class CostConfig
    {
        public decimal MonthlyBudget { get; set; }
        public bool EnableSpotInstances { get; set; }
        public bool EnableAutoShutdown { get; set; }
        public string ShutdownSchedule { get; set; }
    }
}
