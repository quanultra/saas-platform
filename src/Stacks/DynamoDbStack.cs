using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using DynamoAttribute = Amazon.CDK.AWS.DynamoDB.Attribute;
using Amazon.CDK.AWS.Backup;
using Amazon.CDK.AWS.Events;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// DynamoDB Stack with tables, GSIs, LSIs, auto-scaling, and backups
    /// Implements Requirement 3.15
    /// </summary>
    public class DynamoDbStack : BaseStack
    {
        public Table MainTable { get; }
        public Table EventsTable { get; }
        public BackupPlan BackupPlan { get; }

        public DynamoDbStack(Construct scope, string id, IStackProps props, StackConfiguration config)
            : base(scope, id, props, config)
        {
            // Create main table with GSI and LSI
            MainTable = new Table(this, "MainTable", new TableProps
            {
                TableName = GenerateResourceName("main-table"),
                PartitionKey = new DynamoAttribute { Name = "pk", Type = AttributeType.STRING },
                SortKey = new DynamoAttribute { Name = "sk", Type = AttributeType.STRING },
                BillingMode = BillingMode.PROVISIONED,
                ReadCapacity = 5,
                WriteCapacity = 5,
                Encryption = TableEncryption.AWS_MANAGED,
                Stream = StreamViewType.NEW_AND_OLD_IMAGES,
                RemovalPolicy = RemovalPolicy.RETAIN,
                DeletionProtection = config.Environment == "prod"
            });

            // Add Global Secondary Index (GSI)
            MainTable.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
            {
                IndexName = "gsi-status-timestamp",
                PartitionKey = new DynamoAttribute { Name = "status", Type = AttributeType.STRING },
                SortKey = new DynamoAttribute { Name = "timestamp", Type = AttributeType.NUMBER },
                ProjectionType = ProjectionType.ALL,
                ReadCapacity = 5,
                WriteCapacity = 5
            });

            // Add another GSI for different access pattern
            MainTable.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
            {
                IndexName = "gsi-type-created",
                PartitionKey = new DynamoAttribute { Name = "type", Type = AttributeType.STRING },
                SortKey = new DynamoAttribute { Name = "createdAt", Type = AttributeType.STRING },
                ProjectionType = ProjectionType.KEYS_ONLY,
                ReadCapacity = 3,
                WriteCapacity = 3
            });

            // Add Local Secondary Index (LSI)
            MainTable.AddLocalSecondaryIndex(new LocalSecondaryIndexProps
            {
                IndexName = "lsi-updated",
                SortKey = new DynamoAttribute { Name = "updatedAt", Type = AttributeType.STRING },
                ProjectionType = ProjectionType.ALL
            });

            // Configure auto-scaling for main table
            var readScaling = MainTable.AutoScaleReadCapacity(new EnableScalingProps { MinCapacity = 5, MaxCapacity = 100 });
            readScaling.ScaleOnUtilization(new UtilizationScalingProps { TargetUtilizationPercent = 70 });

            var writeScaling = MainTable.AutoScaleWriteCapacity(new EnableScalingProps { MinCapacity = 5, MaxCapacity = 100 });
            writeScaling.ScaleOnUtilization(new UtilizationScalingProps { TargetUtilizationPercent = 70 });

            // Create events table for event sourcing pattern
            EventsTable = new Table(this, "EventsTable", new TableProps
            {
                TableName = GenerateResourceName("events-table"),
                PartitionKey = new DynamoAttribute { Name = "aggregateId", Type = AttributeType.STRING },
                SortKey = new DynamoAttribute { Name = "version", Type = AttributeType.NUMBER },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                Encryption = TableEncryption.AWS_MANAGED,
                Stream = StreamViewType.NEW_IMAGE,
                RemovalPolicy = RemovalPolicy.RETAIN,
                TimeToLiveAttribute = "ttl"
            });

            // Add GSI for events table
            EventsTable.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
            {
                IndexName = "gsi-event-type",
                PartitionKey = new DynamoAttribute { Name = "eventType", Type = AttributeType.STRING },
                SortKey = new DynamoAttribute { Name = "timestamp", Type = AttributeType.NUMBER },
                ProjectionType = ProjectionType.ALL
            });

            // Create backup plan for DynamoDB tables
            BackupPlan = new BackupPlan(this, "DynamoDbBackupPlan", new BackupPlanProps
            {
                BackupPlanName = GenerateResourceName("dynamodb-backup"),
                BackupPlanRules = new[]
                {
                    new BackupPlanRule(new BackupPlanRuleProps
                    {
                        RuleName = "DailyBackup",
                        DeleteAfter = Duration.Days(30),
                        ScheduleExpression = Schedule.Cron(new CronOptions { Hour = "2", Minute = "0" })
                    }),
                    new BackupPlanRule(new BackupPlanRuleProps
                    {
                        RuleName = "WeeklyBackup",
                        DeleteAfter = Duration.Days(90),
                        ScheduleExpression = Schedule.Cron(new CronOptions { Hour = "3", Minute = "0", WeekDay = "SUN" })
                    })
                }
            });

            // Add tables to backup plan
            BackupPlan.AddSelection("DynamoDbBackupSelection", new BackupSelectionOptions
            {
                Resources = new[] { BackupResource.FromDynamoDbTable(MainTable), BackupResource.FromDynamoDbTable(EventsTable) }
            });

            // Create CloudFormation outputs
            CreateOutput("MainTableName", MainTable.TableName, "Main DynamoDB table name");
            CreateOutput("MainTableArn", MainTable.TableArn, "Main DynamoDB table ARN");
            CreateOutput("MainTableStreamArn", MainTable.TableStreamArn ?? "N/A", "Main table stream ARN");
            CreateOutput("EventsTableName", EventsTable.TableName, "Events DynamoDB table name");
            CreateOutput("EventsTableArn", EventsTable.TableArn, "Events DynamoDB table ARN");
            CreateOutput("EventsTableStreamArn", EventsTable.TableStreamArn ?? "N/A", "Events table stream ARN");
            CreateOutput("BackupPlanId", BackupPlan.BackupPlanId, "DynamoDB backup plan ID");
        }
    }
}
