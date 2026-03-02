using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Models
{
    /// <summary>
    /// Configuration model for stack deployment
    /// </summary>
    public class StackConfiguration
    {
        public string Environment { get; set; } = "dev";
        public string ProjectName { get; set; } = "aws-sap-c02-practice";
        public Dictionary<string, string> Tags { get; set; } = new();
        public NetworkConfiguration Network { get; set; } = new();
        public DatabaseConfiguration Database { get; set; } = new();
        public SecurityConfiguration Security { get; set; } = new();
        public MultiRegionConfig MultiRegion { get; set; } = new();
        public MonitoringConfiguration Monitoring { get; set; } = new();
    }

    public class NetworkConfiguration
    {
        public string PrimaryCidr { get; set; } = "10.0.0.0/16";
        public string SecondaryCidr { get; set; } = "10.1.0.0/16";
        public int MaxAzs { get; set; } = 3;
        public bool EnableNatGateway { get; set; } = true;
    }

    public class DatabaseConfiguration
    {
        public string Engine { get; set; } = "aurora-postgresql";
        public string InstanceClass { get; set; } = "db.r5.large";
        public bool EnableEncryption { get; set; } = true;
        public int BackupRetentionDays { get; set; } = 7;
        public string DatabaseName { get; set; } = "sapc02db";
    }

    public class SecurityConfiguration
    {
        public bool EnableWaf { get; set; } = true;
        public bool EnableGuardDuty { get; set; } = true;
        public bool EnableSecurityHub { get; set; } = true;
        public List<string> AllowedCidrs { get; set; } = new();
    }

    public class MultiRegionConfig
    {
        public string PrimaryRegion { get; set; } = "us-east-1";
        public string SecondaryRegion { get; set; } = "eu-west-1";
        public bool EnableCrossRegionReplication { get; set; } = true;
    }

    public class MonitoringConfiguration
    {
        public string AlarmEmail { get; set; } = "";
        public bool EnableXRay { get; set; } = true;
        public bool EnableContainerInsights { get; set; } = true;
        public int LogRetentionDays { get; set; } = 30;
    }
}
