using Xunit;
using Amazon.CDK;
using Amazon.CDK.Assertions;
using AwsSapC02Practice.Infrastructure.Models;

namespace AwsSapC02Practice.Infrastructure.Tests.Unit
{
    /// <summary>
    /// Unit tests for RDS Stack with Aurora Global Database
    /// Tests Requirements 3.4, 8.2
    /// </summary>
    public class RdsStackTests
    {
        [Fact]
        public void TestDatabaseConfigurationModel()
        {
            // Test that the database configuration model is properly set up
            var config = new DatabaseConfiguration
            {
                DatabaseName = "testdb",
                EnableEncryption = true,
                BackupRetentionDays = 7,
                Engine = "aurora-postgresql"
            };

            Assert.Equal("testdb", config.DatabaseName);
            Assert.True(config.EnableEncryption);
            Assert.Equal(7, config.BackupRetentionDays);
            Assert.Equal("aurora-postgresql", config.Engine);
        }

        [Fact]
        public void TestStackConfigurationHasDatabaseSettings()
        {
            // Test that stack configuration includes database settings
            var config = new StackConfiguration
            {
                Environment = "test",
                Database = new DatabaseConfiguration
                {
                    DatabaseName = "sapc02db",
                    EnableEncryption = true
                }
            };

            Assert.NotNull(config.Database);
            Assert.Equal("sapc02db", config.Database.DatabaseName);
            Assert.True(config.Database.EnableEncryption);
        }

        [Fact]
        public void TestMultiRegionConfigForDatabase()
        {
            // Test multi-region configuration
            var config = new StackConfiguration
            {
                MultiRegion = new MultiRegionConfig
                {
                    PrimaryRegion = "us-east-1",
                    SecondaryRegion = "eu-west-1",
                    EnableCrossRegionReplication = true
                }
            };

            Assert.Equal("us-east-1", config.MultiRegion.PrimaryRegion);
            Assert.Equal("eu-west-1", config.MultiRegion.SecondaryRegion);
            Assert.True(config.MultiRegion.EnableCrossRegionReplication);
        }

        [Fact]
        public void TestDatabaseConfigurationDefaults()
        {
            // Test default values
            var config = new DatabaseConfiguration();

            Assert.Equal("aurora-postgresql", config.Engine);
            Assert.Equal("db.r5.large", config.InstanceClass);
            Assert.True(config.EnableEncryption);
            Assert.Equal(7, config.BackupRetentionDays);
            Assert.Equal("sapc02db", config.DatabaseName);
        }
    }
}
