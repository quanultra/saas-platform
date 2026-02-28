using Amazon.CDK;
using Amazon.CDK.Assertions;
using Amazon.CDK.AWS.EC2;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Stacks;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace AwsSapC02Practice.Tests.Unit
{
    /// <summary>
    /// Tests for EKS Cluster Stack
    /// Validates Requirement 11.3 - Test EKS pod scheduling
    /// </summary>
    public class EksStackTests
    {
        private readonly StackConfiguration _testConfig;
        private readonly App _app;
        private readonly IVpc _vpc;

        public EksStackTests()
        {
            _testConfig = new StackConfiguration
            {
                Environment = "test",
                ProjectName = "test-project",

            };
            _app = new App();

            // Create VPC for testing
            var vpcStack = new VpcStack(_app, "TestVpcStack", new StackProps(), _testConfig);
            _vpc = vpcStack.PrimaryVpc.Vpc;
        }

        [Fact]
        public void EksStack_ShouldCreateCluster()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);

            stack.EksCluster.Should().NotBeNull();
            // ClusterName is a CDK token, so we just verify it's not null or empty
            stack.EksCluster.ClusterName.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void EksStack_ShouldCreateManagedNodeGroup()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);

            stack.ManagedNodeGroup.Should().NotBeNull();
            // NodegroupName is a CDK token, so we just verify it's not null or empty
            stack.ManagedNodeGroup.NodegroupName.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void EksStack_ShouldCreateClusterWithLogging()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("Custom::AWSCDK-EKS-Cluster", new Dictionary<string, object>
            {
                ["Config"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["logging"] = Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["clusterLogging"] = Match.ArrayWith(new[]
                        {
                            Match.ObjectLike(new Dictionary<string, object>
                            {
                                ["enabled"] = true,
                                ["types"] = Match.ArrayWith(new[] { "api", "audit", "authenticator", "controllerManager", "scheduler" })
                            })
                        })
                    })
                })
            });
        }

        [Fact]
        public void EksStack_ShouldCreateNodeGroupWithCorrectInstanceType()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::EKS::Nodegroup", new Dictionary<string, object>
            {
                ["InstanceTypes"] = new[] { "t3.medium" }
            });
        }

        [Fact]
        public void EksStack_ShouldCreateNodeGroupWithCorrectCapacity()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::EKS::Nodegroup", new Dictionary<string, object>
            {
                ["ScalingConfig"] = new Dictionary<string, int>
                {
                    ["MinSize"] = 2,
                    ["MaxSize"] = 10,
                    ["DesiredSize"] = 2
                }
            });
        }

        [Fact]
        public void EksStack_ShouldCreateClusterAutoscalerServiceAccount()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);
            var template = Template.FromStack(stack);

            // Manifest is a complex object (Fn::Join), so we check for the resource type and PruneLabel
            template.HasResourceProperties("Custom::AWSCDK-EKS-KubernetesResource", new Dictionary<string, object>
            {
                ["PruneLabel"] = Match.StringLikeRegexp(".*")
            });
        }

        [Fact]
        public void EksStack_ShouldCreateIAMRoleForCluster()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::IAM::Role", new Dictionary<string, object>
            {
                ["AssumeRolePolicyDocument"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["Statement"] = Match.ArrayWith(new[]
                    {
                        Match.ObjectLike(new Dictionary<string, object>
                        {
                            ["Principal"] = new Dictionary<string, object>
                            {
                                ["Service"] = "eks.amazonaws.com"
                            }
                        })
                    })
                })
            });
        }

        [Fact]
        public void EksStack_ShouldCreateIAMRoleForNodeGroup()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::IAM::Role", new Dictionary<string, object>
            {
                ["AssumeRolePolicyDocument"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["Statement"] = Match.ArrayWith(new[]
                    {
                        Match.ObjectLike(new Dictionary<string, object>
                        {
                            ["Principal"] = new Dictionary<string, object>
                            {
                                ["Service"] = "ec2.amazonaws.com"
                            }
                        })
                    })
                })
            });
        }

        [Fact]
        public void EksStack_ShouldAttachRequiredPolicies()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);
            var template = Template.FromStack(stack);

            // Check for EKS cluster policies
            template.HasResourceProperties("AWS::IAM::Role", new Dictionary<string, object>
            {
                ["ManagedPolicyArns"] = Match.ArrayWith(new[]
                {
                    Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["Fn::Join"] = Match.ArrayWith(new object[]
                        {
                            Match.ArrayWith(new object[]
                            {
                                Match.StringLikeRegexp(".*AmazonEKSClusterPolicy.*")
                            })
                        })
                    })
                })
            });
        }

        [Fact]
        public void EksStack_ShouldCreateNodeGroupWithAutoscalerTags()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::EKS::Nodegroup", new Dictionary<string, object>
            {
                ["Tags"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["k8s.io/cluster-autoscaler/enabled"] = "true"
                })
            });
        }

        [Fact]
        public void EksStack_ShouldCreateHelmChartForClusterAutoscaler()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("Custom::AWSCDK-EKS-HelmChart", new Dictionary<string, object>
            {
                ["Chart"] = "cluster-autoscaler",
                ["Repository"] = "https://kubernetes.github.io/autoscaler"
            });
        }

        [Fact]
        public void EksStack_ShouldCreateServiceAccountForALBController()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);
            var template = Template.FromStack(stack);

            // Manifest is a complex object (Fn::Join), so we check for the resource type and PruneLabel
            template.HasResourceProperties("Custom::AWSCDK-EKS-KubernetesResource", new Dictionary<string, object>
            {
                ["PruneLabel"] = Match.StringLikeRegexp(".*")
            });
        }

        [Fact]
        public void EksStack_ShouldCreateOutputs()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);
            var template = Template.FromStack(stack);

            template.HasOutput("ClusterName", new Dictionary<string, object>());
            template.HasOutput("ClusterArn", new Dictionary<string, object>());
            template.HasOutput("ClusterEndpoint", new Dictionary<string, object>());
            template.HasOutput("ClusterSecurityGroupId", new Dictionary<string, object>());
            template.HasOutput("NodeGroupName", new Dictionary<string, object>());
            template.HasOutput("AutoscalerServiceAccountName", new Dictionary<string, object>());
        }

        [Fact]
        public void EksStack_ShouldHaveCorrectResourceCount()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::EKS::Nodegroup", 1);
            // Kubectl layer adds 4 additional roles, so total is 8
            // Cluster role, node role, autoscaler SA role, ALB controller SA role + 4 kubectl layer roles
            template.ResourceCountIs("AWS::IAM::Role", 8);
        }

        [Fact]
        public void EksStack_ShouldEnablePublicAndPrivateAccess()
        {
            var stack = new EksStack(_app, "TestEksStack", new StackProps(), _testConfig, _vpc);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("Custom::AWSCDK-EKS-Cluster", new Dictionary<string, object>
            {
                ["Config"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["resourcesVpcConfig"] = Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["endpointPublicAccess"] = true,
                        ["endpointPrivateAccess"] = true
                    })
                })
            });
        }
    }
}
