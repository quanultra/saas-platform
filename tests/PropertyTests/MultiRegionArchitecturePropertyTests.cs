using Amazon.CDK;
using Amazon.CDK.Assertions;
using AwsSapC02Practice.Infrastructure.Constructs.Network;
using AwsSapC02Practice.Infrastructure.Constructs.Storage;
using AwsSapC02Practice.Infrastructure.Constructs.Database;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Xunit;

namespace AwsSapC02Practice.Tests.PropertyTests
{
    public class MultiRegionArchitecturePropertyTests
    {
        [FsCheck.Xunit.Property(MaxTest = 100)]
        public FsCheck.Property VpcCidrBlocksShouldNotOverlapBetweenRegions()
        {
            var cidrList = new[] { "10.0.0.0/16", "10.1.0.0/16", "10.2.0.0/16", "172.16.0.0/16", "192.168.0.0/16" };
            var cidrGenerator = Gen.Elements(cidrList);
            var regionPairGenerator = from primaryCidr in cidrGenerator from secondaryCidr in cidrGenerator select new { PrimaryCidr = primaryCidr, SecondaryCidr = secondaryCidr };
            return Prop.ForAll(Arb.From(regionPairGenerator), pair =>
            {
                var app = new App();
                var stack = new Stack(app, "TestStack");
                var primaryVpc = new MultiRegionVpc(stack, "PrimaryVpc", new MultiRegionVpcProps { Environment = "test", Region = "us-east-1", CidrBlock = pair.PrimaryCidr, MaxAzs = 3, EnableNatGateway = true });
                var secondaryVpc = new MultiRegionVpc(stack, "SecondaryVpc", new MultiRegionVpcProps { Environment = "test", Region = "eu-west-1", CidrBlock = pair.SecondaryCidr, MaxAzs = 3, EnableNatGateway = true });
                var primaryNetwork = IPNetwork.Parse(pair.PrimaryCidr);
                var secondaryNetwork = IPNetwork.Parse(pair.SecondaryCidr);
                var overlaps = primaryNetwork.Overlap(secondaryNetwork);
                return (pair.PrimaryCidr == pair.SecondaryCidr) || !overlaps;
            });
        }

        [FsCheck.Xunit.Property(MaxTest = 100)]
        public FsCheck.Property S3ReplicationShouldBeConfiguredForCrossRegion()
        {
            var bucketPrefixGenerator = Gen.Elements("test-bucket", "data-bucket", "backup-bucket");
            var regionPairGenerator = Gen.Elements(new { Primary = "us-east-1", Secondary = "eu-west-1" }, new { Primary = "us-west-2", Secondary = "ap-southeast-1" });
            var testDataGenerator = from prefix in bucketPrefixGenerator from regions in regionPairGenerator select new { BucketPrefix = prefix, Regions = regions };
            return Prop.ForAll(Arb.From(testDataGenerator), data =>
            {
                var app = new App();
                var stack = new Stack(app, "TestStack");
                var crossRegionS3 = new CrossRegionS3(stack, "CrossRegionS3", new CrossRegionS3Props { BucketPrefix = data.BucketPrefix, PrimaryRegion = data.Regions.Primary, SecondaryRegion = data.Regions.Secondary });
                crossRegionS3.PrimaryBucket.Should().NotBeNull();
                crossRegionS3.SecondaryBucket.Should().NotBeNull();
                crossRegionS3.ReplicationRole.Should().NotBeNull();
                var template = Template.FromStack(stack);
                var buckets = template.FindResources("AWS::S3::Bucket");
                var primaryBucket = buckets.FirstOrDefault(b => { var props = b.Value as Dictionary<string, object>; if (props != null && props.ContainsKey("Properties")) { var properties = props["Properties"] as Dictionary<string, object>; return properties != null && properties.ContainsKey("ReplicationConfiguration"); } return false; });
                return primaryBucket.Key != null;
            });
        }

        [FsCheck.Xunit.Property(MaxTest = 100)]
        public FsCheck.Property AuroraGlobalDatabaseShouldSupportFastFailover()
        {
            var databaseNameGenerator = Gen.Elements("testdb", "appdb", "maindb");
            var environmentGenerator = Gen.Elements("dev", "staging", "prod");
            var testDataGenerator = from dbName in databaseNameGenerator from env in environmentGenerator select new { DatabaseName = dbName, Environment = env };
            return Prop.ForAll(Arb.From(testDataGenerator), data =>
            {
                var app = new App();
                var stack = new Stack(app, "TestStack");
                var primaryVpc = new MultiRegionVpc(stack, "TestVpc", new MultiRegionVpcProps { Environment = data.Environment, Region = "us-east-1", CidrBlock = "10.0.0.0/16", MaxAzs = 3, EnableNatGateway = false });
                var auroraDb = new AuroraGlobalDatabase(stack, "AuroraGlobalDb", new AuroraGlobalDatabaseProps { Vpc = primaryVpc.Vpc, SecurityGroup = primaryVpc.DatabaseSecurityGroup, DatabaseName = data.DatabaseName, Environment = data.Environment, IsPrimaryRegion = true, BackupRetentionDays = 7, EnableEncryption = true });
                auroraDb.PrimaryCluster.Should().NotBeNull();
                auroraDb.GlobalClusterIdentifier.Should().NotBeNullOrEmpty();
                var template = Template.FromStack(stack);
                var clusters = template.FindResources("AWS::RDS::DBCluster");
                var clusterCount = clusters.Count;
                var hasBackupRetention = clusters.Any(c => { var props = c.Value as Dictionary<string, object>; if (props != null && props.ContainsKey("Properties")) { var properties = props["Properties"] as Dictionary<string, object>; if (properties != null && properties.ContainsKey("BackupRetentionPeriod")) { var retention = Convert.ToInt32(properties["BackupRetentionPeriod"]); return retention >= 1; } } return false; });
                var hasEncryption = clusters.Any(c => { var props = c.Value as Dictionary<string, object>; if (props != null && props.ContainsKey("Properties")) { var properties = props["Properties"] as Dictionary<string, object>; if (properties != null && properties.ContainsKey("StorageEncrypted")) { return Convert.ToBoolean(properties["StorageEncrypted"]); } } return false; });
                var instances = template.FindResources("AWS::RDS::DBInstance");
                var hasMultipleInstances = instances.Count >= 2;
                return clusterCount > 0 && hasBackupRetention && hasEncryption && hasMultipleInstances;
            });
        }
    }

    public class IPNetwork
    {
        public IPAddress Network { get; }
        public int PrefixLength { get; }
        public IPAddress Netmask { get; }
        private IPNetwork(IPAddress network, int prefixLength) { Network = network; PrefixLength = prefixLength; Netmask = CalculateNetmask(prefixLength); }
        public static IPNetwork Parse(string cidr) { var parts = cidr.Split('/'); var network = IPAddress.Parse(parts[0]); var prefixLength = int.Parse(parts[1]); return new IPNetwork(network, prefixLength); }
        public bool Overlap(IPNetwork other) { var thisStart = IPToUInt32(Network); var thisEnd = thisStart + (uint)Math.Pow(2, 32 - PrefixLength) - 1; var otherStart = IPToUInt32(other.Network); var otherEnd = otherStart + (uint)Math.Pow(2, 32 - other.PrefixLength) - 1; return (thisStart <= otherEnd && thisEnd >= otherStart); }
        private static uint IPToUInt32(IPAddress ip) { var bytes = ip.GetAddressBytes(); if (BitConverter.IsLittleEndian) Array.Reverse(bytes); return BitConverter.ToUInt32(bytes, 0); }
        private static IPAddress CalculateNetmask(int prefixLength) { uint mask = 0xFFFFFFFF << (32 - prefixLength); byte[] bytes = BitConverter.GetBytes(mask); if (BitConverter.IsLittleEndian) Array.Reverse(bytes); return new IPAddress(bytes); }
    }
}
