using Amazon.CDK;
using System;

namespace AwsSapC02Practice.Infrastructure
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            // TODO: Add stacks here as we implement them
            // Example:
            // new MultiRegionStack(app, "MultiRegionStack", new StackProps
            // {
            //     Env = new Environment
            //     {
            //         Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
            //         Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
            //     }
            // });

            app.Synth();
        }
    }
}
