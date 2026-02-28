using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Constructs.DisasterRecovery;
using AwsSapC02Practice.Infrastructure.Constructs.Network;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// Pilot Light DR Stack
    /// Implements minimal infrastructure in DR region for quick recovery
    /// Implements Requirements 7.2, 8.5
    /// </summary>
    public class PilotLightStack : BaseStack
    {
        public PilotLight PilotLight { get; }
        public IVpc DrVpc { get; }

        public PilotLightStack(Construct scope, string id, IStackProps props, StackConfiguration config)
            : base(scope, id, props, config)
        {
            // Create VPC in DR region
            DrVpc = new MultiRegionVpc(this, "DrVpc", new MultiRegionVpcProps
            {
                Environment = config.Environment,
                Region = config.MultiRegion.SecondaryRegion,
                CidrBlock = config.Network.SecondaryCidr,
                MaxAzs = config.Network.MaxAzs
            }).Vpc;

            // Create Pilot Light infrastructure
            PilotLight = new PilotLight(this, "PilotLight", new PilotLightProps
            {
                Vpc = DrVpc,
                DatabaseName = config.Database.DatabaseName,
                CreateReadReplica = true
            });

            // Create CloudFormation outputs
            CreateOutput(
                "DrVpcId",
                DrVpc.VpcId,
                "VPC ID in DR region"
            );

            CreateOutput(
                "StandbyDbEndpoint",
                PilotLight.StandbyDatabase.DbInstanceEndpointAddress,
                "Standby database endpoint in DR region"
            );

            CreateOutput(
                "StandbyDbArn",
                PilotLight.StandbyDatabase.InstanceArn,
                "ARN of standby database"
            );

            CreateOutput(
                "FailoverFunctionArn",
                PilotLight.FailoverFunction.FunctionArn,
                "ARN of failover Lambda function"
            );

            CreateOutput(
                "MinimalAsgName",
                PilotLight.MinimalAsg.AutoScalingGroupName,
                "Name of minimal Auto Scaling Group"
            );

            CreateOutput(
                "DrRegion",
                config.MultiRegion.SecondaryRegion,
                "DR region name"
            );
        }
    }
}
