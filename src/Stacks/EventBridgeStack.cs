using Amazon.CDK;
using AwsEvents = Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AWS.SNS;
using AwsLogs = Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.EventSchemas;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// EventBridge Stack with event buses, rules, and targets
    /// Implements Requirement 3.17
    /// </summary>
    public class EventBridgeStack : BaseStack
    {
        public AwsEvents.EventBus CustomEventBus { get; }
        public AwsEvents.EventBus ApplicationEventBus { get; }
        public AwsEvents.Rule OrderProcessingRule { get; }
        public AwsEvents.Rule ScheduledRule { get; }
        public CfnRegistry SchemaRegistry { get; }
        public Function EventHandlerFunction { get; }
        public Queue DeadLetterQueue { get; }

        public EventBridgeStack(Construct scope, string id, IStackProps props, StackConfiguration config)
            : base(scope, id, props, config)
        {
            // Create custom event buses
            CustomEventBus = new AwsEvents.EventBus(this, "CustomEventBus", new AwsEvents.EventBusProps { EventBusName = GenerateResourceName("custom-events") });
            ApplicationEventBus = new AwsEvents.EventBus(this, "ApplicationEventBus", new AwsEvents.EventBusProps { EventBusName = GenerateResourceName("app-events") });

            // Create dead letter queue for failed events
            DeadLetterQueue = new Queue(this, "EventDLQ", new QueueProps
            {
                QueueName = GenerateResourceName("event-dlq"),
                RetentionPeriod = Duration.Days(14),
                Encryption = QueueEncryption.KMS_MANAGED
            });

            // Create target queue for events
            var targetQueue = new Queue(this, "EventTargetQueue", new QueueProps
            {
                QueueName = GenerateResourceName("event-target"),
                VisibilityTimeout = Duration.Seconds(300),
                DeadLetterQueue = new DeadLetterQueue { Queue = DeadLetterQueue, MaxReceiveCount = 3 }
            });

            // Create SNS topic for notifications
            var notificationTopic = new Topic(this, "NotificationTopic", new TopicProps
            {
                TopicName = GenerateResourceName("notifications"),
                DisplayName = "Event notifications topic"
            });

            // Create Lambda function for event handling
            EventHandlerFunction = new Function(this, "EventHandlerFunction", new FunctionProps
            {
                Runtime = Runtime.NODEJS_20_X,
                Handler = "index.handler",
                Code = Code.FromInline("exports.handler = async (event) => { console.log('Received event:', JSON.stringify(event, null, 2)); const detail = event.detail || {}; const eventType = detail.type || 'unknown'; switch(eventType) { case 'order.created': console.log('Processing new order:', detail.orderId); break; case 'order.updated': console.log('Processing order update:', detail.orderId); break; case 'order.cancelled': console.log('Processing order cancellation:', detail.orderId); break; default: console.log('Unknown event type:', eventType); } return { statusCode: 200, body: JSON.stringify({ message: 'Event processed successfully' }) }; };"),
                FunctionName = GenerateResourceName("event-handler"),
                Timeout = Duration.Seconds(30),
                MemorySize = 256,
                DeadLetterQueue = DeadLetterQueue
            });

            // Create log group for EventBridge
            var logGroup = new AwsLogs.LogGroup(this, "EventBridgeLogGroup", new AwsLogs.LogGroupProps
            {
                LogGroupName = $"/aws/events/{GenerateResourceName("events")}",
                Retention = AwsLogs.RetentionDays.ONE_WEEK,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // Create rule for order processing events
            OrderProcessingRule = new AwsEvents.Rule(this, "OrderProcessingRule", new AwsEvents.RuleProps
            {
                RuleName = GenerateResourceName("order-processing"),
                Description = "Route order events to appropriate targets",
                EventBus = CustomEventBus,
                EventPattern = new AwsEvents.EventPattern
                {
                    Source = new[] { "custom.orders" },
                    DetailType = new[] { "Order Created", "Order Updated", "Order Cancelled" },
                    Detail = new Dictionary<string, object> { ["status"] = new[] { "pending", "processing", "completed" } }
                }
            });

            // Add multiple targets to the rule
            OrderProcessingRule.AddTarget(new LambdaFunction(EventHandlerFunction, new LambdaFunctionProps { DeadLetterQueue = DeadLetterQueue, MaxEventAge = Duration.Hours(2), RetryAttempts = 2 }));
            OrderProcessingRule.AddTarget(new SqsQueue(targetQueue, new SqsQueueProps { MessageGroupId = "order-events" }));
            OrderProcessingRule.AddTarget(new SnsTopic(notificationTopic));
            OrderProcessingRule.AddTarget(new CloudWatchLogGroup(logGroup));

            // Create scheduled rule (cron-based)
            ScheduledRule = new AwsEvents.Rule(this, "ScheduledRule", new AwsEvents.RuleProps
            {
                RuleName = GenerateResourceName("scheduled-task"),
                Description = "Trigger scheduled tasks every 5 minutes",
                EventBus = ApplicationEventBus,
                Schedule = AwsEvents.Schedule.Rate(Duration.Minutes(5))
            });

            ScheduledRule.AddTarget(new LambdaFunction(EventHandlerFunction));

            // Create rule for application events
            var applicationRule = new AwsEvents.Rule(this, "ApplicationRule", new AwsEvents.RuleProps
            {
                RuleName = GenerateResourceName("app-events"),
                Description = "Handle application-level events",
                EventBus = ApplicationEventBus,
                EventPattern = new AwsEvents.EventPattern { Source = new[] { "custom.application" }, DetailType = new[] { "User Action", "System Event" } }
            });

            applicationRule.AddTarget(new LambdaFunction(EventHandlerFunction));

            // Create Schema Registry
            SchemaRegistry = new CfnRegistry(this, "SchemaRegistry", new CfnRegistryProps
            {
                RegistryName = GenerateResourceName("schema-registry"),
                Description = "Schema registry for event validation"
            });

            // Create a sample schema
            var orderSchema = new CfnSchema(this, "OrderSchema", new CfnSchemaProps
            {
                RegistryName = SchemaRegistry.RegistryName,
                SchemaName = "OrderEvent",
                Type = "OpenApi3",
                Description = "Schema for order events",
                Content = "{\"openapi\":\"3.0.0\",\"info\":{\"version\":\"1.0.0\",\"title\":\"Order Event\"},\"paths\":{},\"components\":{\"schemas\":{\"OrderEvent\":{\"type\":\"object\",\"required\":[\"orderId\",\"type\",\"timestamp\"],\"properties\":{\"orderId\":{\"type\":\"string\"},\"type\":{\"type\":\"string\",\"enum\":[\"order.created\",\"order.updated\",\"order.cancelled\"]},\"timestamp\":{\"type\":\"string\",\"format\":\"date-time\"},\"status\":{\"type\":\"string\"}}}}}}"
            });

            orderSchema.AddDependency(SchemaRegistry);

            // Create CloudFormation outputs
            CreateOutput("CustomEventBusName", CustomEventBus.EventBusName, "Custom event bus name");
            CreateOutput("CustomEventBusArn", CustomEventBus.EventBusArn, "Custom event bus ARN");
            CreateOutput("ApplicationEventBusName", ApplicationEventBus.EventBusName, "Application event bus name");
            CreateOutput("ApplicationEventBusArn", ApplicationEventBus.EventBusArn, "Application event bus ARN");
            CreateOutput("OrderProcessingRuleName", OrderProcessingRule.RuleName, "Order processing rule name");
            CreateOutput("EventHandlerFunctionArn", EventHandlerFunction.FunctionArn, "Event handler Lambda function ARN");
            CreateOutput("SchemaRegistryName", SchemaRegistry.RegistryName ?? "N/A", "Schema registry name");
            CreateOutput("TargetQueueUrl", targetQueue.QueueUrl, "Event target queue URL");
            CreateOutput("DeadLetterQueueUrl", DeadLetterQueue.QueueUrl, "Dead letter queue URL");
        }
    }
}
