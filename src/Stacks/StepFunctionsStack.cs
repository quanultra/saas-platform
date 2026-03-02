using Amazon.CDK;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.IAM;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// Step Functions Stack with state machines
    /// Implements Requirement 3.16
    /// </summary>
    public class StepFunctionsStack : BaseStack
    {
        public StateMachine StandardWorkflow { get; }
        public StateMachine ExpressWorkflow { get; }
        public Function ProcessTaskFunction { get; }
        public Function ValidateTaskFunction { get; }

        public StepFunctionsStack(Construct scope, string id, IStackProps props, StackConfiguration config)
            : base(scope, id, props, config)
        {
            // Create Lambda functions for workflow tasks
            ProcessTaskFunction = new Function(this, "ProcessTaskFunction", new FunctionProps
            {
                Runtime = Runtime.NODEJS_20_X,
                Handler = "index.handler",
                Code = Code.FromInline("exports.handler = async (event) => { console.log('Processing task:', JSON.stringify(event)); if (event.shouldFail) { throw new Error('Task processing failed'); } return { ...event, processed: true, timestamp: new Date().toISOString() }; };"),
                FunctionName = GenerateResourceName("process-task"),
                Timeout = Duration.Seconds(30),
                MemorySize = 256
            });

            ValidateTaskFunction = new Function(this, "ValidateTaskFunction", new FunctionProps
            {
                Runtime = Runtime.NODEJS_20_X,
                Handler = "index.handler",
                Code = Code.FromInline("exports.handler = async (event) => { console.log('Validating task:', JSON.stringify(event)); const isValid = event.processed && event.timestamp; return { ...event, validated: isValid, validationTime: new Date().toISOString() }; };"),
                FunctionName = GenerateResourceName("validate-task"),
                Timeout = Duration.Seconds(15),
                MemorySize = 128
            });

            // Create log group for Step Functions
            var logGroup = new LogGroup(this, "StepFunctionsLogGroup", new LogGroupProps
            {
                LogGroupName = $"/aws/stepfunctions/{GenerateResourceName("workflows")}",
                Retention = RetentionDays.ONE_WEEK,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // Define Standard Workflow with error handling and retries
            var processTask = new LambdaInvoke(this, "ProcessTask", new LambdaInvokeProps
            {
                LambdaFunction = ProcessTaskFunction,
                OutputPath = "$.Payload",
                RetryOnServiceExceptions = true
            });

            // Add retry configuration
            processTask.AddRetry(new RetryProps
            {
                Errors = new[] { "States.TaskFailed", "States.Timeout" },
                Interval = Duration.Seconds(2),
                MaxAttempts = 3,
                BackoffRate = 2.0
            });

            // Add catch for error handling
            var errorHandler = new Pass(this, "HandleError", new PassProps
            {
                Result = Result.FromObject(new Dictionary<string, object> { ["error"] = "Processing failed after retries", ["status"] = "failed" })
            });

            processTask.AddCatch(errorHandler, new CatchProps { Errors = new[] { "States.ALL" }, ResultPath = "$.error" });

            var validateTask = new LambdaInvoke(this, "ValidateTask", new LambdaInvokeProps
            {
                LambdaFunction = ValidateTaskFunction,
                OutputPath = "$.Payload"
            });

            var checkValidation = new Choice(this, "CheckValidation")
                .When(Condition.BooleanEquals("$.validated", true), new Succeed(this, "Success"))
                .Otherwise(new Fail(this, "ValidationFailed", new FailProps { Error = "ValidationError", Cause = "Task validation failed" }));

            var definition = processTask.Next(validateTask).Next(checkValidation);

            // Create Standard Workflow
            StandardWorkflow = new StateMachine(this, "StandardWorkflow", new StateMachineProps
            {
                StateMachineName = GenerateResourceName("standard-workflow"),
                DefinitionBody = DefinitionBody.FromChainable(definition),
                StateMachineType = StateMachineType.STANDARD,
                Timeout = Duration.Minutes(5),
                TracingEnabled = true,
                Logs = new LogOptions { Destination = logGroup, Level = LogLevel.ALL, IncludeExecutionData = true }
            });

            // Define Express Workflow for high-throughput scenarios
            var expressProcessTask = new LambdaInvoke(this, "ExpressProcessTask", new LambdaInvokeProps
            {
                LambdaFunction = ProcessTaskFunction,
                OutputPath = "$.Payload"
            });

            var expressSuccess = new Succeed(this, "ExpressSuccess");
            var expressDefinition = expressProcessTask.Next(expressSuccess);

            // Create Express Workflow
            ExpressWorkflow = new StateMachine(this, "ExpressWorkflow", new StateMachineProps
            {
                StateMachineName = GenerateResourceName("express-workflow"),
                DefinitionBody = DefinitionBody.FromChainable(expressDefinition),
                StateMachineType = StateMachineType.EXPRESS,
                Timeout = Duration.Seconds(30),
                TracingEnabled = true,
                Logs = new LogOptions { Destination = logGroup, Level = LogLevel.ERROR, IncludeExecutionData = false }
            });

            // Create CloudFormation outputs
            CreateOutput("StandardWorkflowArn", StandardWorkflow.StateMachineArn, "Standard workflow state machine ARN");
            CreateOutput("StandardWorkflowName", StandardWorkflow.StateMachineName, "Standard workflow state machine name");
            CreateOutput("ExpressWorkflowArn", ExpressWorkflow.StateMachineArn, "Express workflow state machine ARN");
            CreateOutput("ExpressWorkflowName", ExpressWorkflow.StateMachineName, "Express workflow state machine name");
            CreateOutput("ProcessTaskFunctionArn", ProcessTaskFunction.FunctionArn, "Process task Lambda function ARN");
            CreateOutput("ValidateTaskFunctionArn", ValidateTaskFunction.FunctionArn, "Validate task Lambda function ARN");
        }
    }
}
