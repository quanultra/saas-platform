using Amazon.CDK;
using Constructs;
using AwsSapC02Practice.Infrastructure.Stacks;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Models
{
    public class StackIntegrationManager
    {
        private readonly Construct _scope;
        private readonly StackConfiguration _config;
        private readonly Dictionary<string, Stack> _stacks = new();

        public StackIntegrationManager(Construct scope, StackConfiguration config)
        {
            _scope = scope;
            _config = config;
        }

        public void RegisterStack(string name, Stack stack)
        {
            _stacks[name] = stack;
        }

        public T GetStack<T>(string name) where T : Stack
        {
            if (_stacks.TryGetValue(name, out var stack))
            {
                return stack as T;
            }
            return null;
        }

        public void WireVpcsWithTransitGateway(VpcStack vpcStack, TransitGatewayStack tgwStack)
        {
            tgwStack.AttachVpc("Primary", vpcStack.PrimaryVpc);
            if (vpcStack.SecondaryVpc != null)
            {
                tgwStack.AttachVpc("Secondary", vpcStack.SecondaryVpc);
            }
        }

        public void WireMonitoringWithResources(MonitoringStack monitoringStack)
        {
            // Monitoring integration placeholder
        }
    }
}
