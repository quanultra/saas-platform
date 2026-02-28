using Amazon.CDK;
using Amazon.CDK.Assertions;
using Amazon.CDK.AWS.ServiceDiscovery;
using AwsSapC02Practice.Infrastructure.Models;
using AwsSapC02Practice.Infrastructure.Stacks;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace AwsSapC02Practice.Tests.Unit
{
    /// <summary>
    /// Tests for AWS App Mesh Stack
    /// Validates Requirement 11.3 - Test App Mesh routing
    /// </summary>
    public class AppMeshStackTests
    {
        private readonly StackConfiguration _testConfig;
        private readonly App _app;
        private readonly IPrivateDnsNamespace _serviceNamespace;

        public AppMeshStackTests()
        {
            _testConfig = new StackConfiguration
            {
                Environment = "test",
                ProjectName = "test-project"
            };
            _app = new App();

            // Create VPC stack to get VPC and namespace
            var vpcStack = new VpcStack(_app, "TestVpcStack", new StackProps(), _testConfig);
            var vpc = vpcStack.PrimaryVpc.Vpc;

            // Create service namespace within the VPC stack
            _serviceNamespace = new PrivateDnsNamespace(vpcStack, "TestNamespace", new PrivateDnsNamespaceProps
            {
                Name = "test.local",
                Vpc = vpc
            });
        }

        [Fact]
        public void AppMeshStack_ShouldCreateMesh()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            stack.Mesh.Should().NotBeNull();
            // Verify mesh is created with correct name pattern
            template.HasResourceProperties("AWS::AppMesh::Mesh", new Dictionary<string, object>
            {
                ["MeshName"] = Match.StringLikeRegexp(".*test-project.*")
            });
        }

        [Fact]
        public void AppMeshStack_ShouldCreateFrontendVirtualNode()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            stack.FrontendVirtualNode.Should().NotBeNull();
            // Verify virtual node is created with correct name
            template.HasResourceProperties("AWS::AppMesh::VirtualNode", new Dictionary<string, object>
            {
                ["VirtualNodeName"] = "frontend-service"
            });
        }

        [Fact]
        public void AppMeshStack_ShouldCreateBackendVirtualNode()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            stack.BackendVirtualNode.Should().NotBeNull();
            // Verify virtual node is created with correct name
            template.HasResourceProperties("AWS::AppMesh::VirtualNode", new Dictionary<string, object>
            {
                ["VirtualNodeName"] = "backend-service"
            });
        }

        [Fact]
        public void AppMeshStack_ShouldCreateBackendVirtualRouter()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            stack.BackendVirtualRouter.Should().NotBeNull();
            // Verify virtual router is created with correct name
            template.HasResourceProperties("AWS::AppMesh::VirtualRouter", new Dictionary<string, object>
            {
                ["VirtualRouterName"] = "backend-router"
            });
        }

        [Fact]
        public void AppMeshStack_ShouldCreateBackendVirtualService()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            stack.BackendVirtualService.Should().NotBeNull();
            // Verify virtual service is created with backend in the name
            template.HasResourceProperties("AWS::AppMesh::VirtualService", new Dictionary<string, object>
            {
                ["VirtualServiceName"] = Match.StringLikeRegexp(".*backend.*")
            });
        }

        [Fact]
        public void AppMeshStack_ShouldCreateMeshWithEgressFilter()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::AppMesh::Mesh", new Dictionary<string, object>
            {
                ["Spec"] = new Dictionary<string, object>
                {
                    ["EgressFilter"] = new Dictionary<string, string>
                    {
                        ["Type"] = "ALLOW_ALL"
                    }
                }
            });
        }

        [Fact]
        public void AppMeshStack_ShouldCreateVirtualNodeWithHealthCheck()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::AppMesh::VirtualNode", new Dictionary<string, object>
            {
                ["Spec"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["Listeners"] = Match.ArrayWith(new[]
                    {
                        Match.ObjectLike(new Dictionary<string, object>
                        {
                            ["HealthCheck"] = Match.ObjectLike(new Dictionary<string, object>
                            {
                                ["HealthyThreshold"] = 2,
                                ["UnhealthyThreshold"] = 3,
                                ["Protocol"] = "http"
                            })
                        })
                    })
                })
            });
        }

        [Fact]
        public void AppMeshStack_ShouldCreateRouteWithRetryPolicy()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::AppMesh::Route", new Dictionary<string, object>
            {
                ["Spec"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["HttpRoute"] = Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["RetryPolicy"] = Match.ObjectLike(new Dictionary<string, object>
                        {
                            ["MaxRetries"] = 3,
                            ["HttpRetryEvents"] = Match.ArrayWith(new[] { "server-error", "gateway-error" })
                        })
                    })
                })
            });
        }

        [Fact]
        public void AppMeshStack_ShouldCreateVirtualGateway()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::AppMesh::VirtualGateway", new Dictionary<string, object>
            {
                ["VirtualGatewayName"] = "ingress-gateway"
            });
        }

        [Fact]
        public void AppMeshStack_ShouldCreateGatewayRoute()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::AppMesh::GatewayRoute", new Dictionary<string, object>
            {
                ["GatewayRouteName"] = "default-route"
            });
        }

        [Fact]
        public void AppMeshStack_ShouldCreateIAMRoleForEnvoy()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
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
                                ["Service"] = "ecs-tasks.amazonaws.com"
                            }
                        })
                    })
                })
            });
        }

        [Fact]
        public void AppMeshStack_ShouldAttachXRayPolicy()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

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
                                Match.StringLikeRegexp(".*AWSXRayDaemonWriteAccess.*")
                            })
                        })
                    })
                })
            });
        }

        [Fact]
        public void AppMeshStack_ShouldCreateServiceDiscoveryServices()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            // Should create 2 service discovery services (frontend and backend)
            template.ResourceCountIs("AWS::ServiceDiscovery::Service", 2);
        }

        [Fact]
        public void AppMeshStack_ShouldConfigureAccessLogging()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            template.HasResourceProperties("AWS::AppMesh::VirtualNode", new Dictionary<string, object>
            {
                ["Spec"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["Logging"] = Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["AccessLog"] = Match.ObjectLike(new Dictionary<string, object>
                        {
                            ["File"] = new Dictionary<string, string>
                            {
                                ["Path"] = "/dev/stdout"
                            }
                        })
                    })
                })
            });
        }

        [Fact]
        public void AppMeshStack_ShouldCreateOutputs()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            template.HasOutput("MeshName", new Dictionary<string, object>());
            template.HasOutput("MeshArn", new Dictionary<string, object>());
            template.HasOutput("FrontendVirtualNodeName", new Dictionary<string, object>());
            template.HasOutput("BackendVirtualNodeName", new Dictionary<string, object>());
            template.HasOutput("BackendVirtualServiceName", new Dictionary<string, object>());
            template.HasOutput("VirtualGatewayName", new Dictionary<string, object>());
            template.HasOutput("EnvoyRoleArn", new Dictionary<string, object>());
        }

        [Fact]
        public void AppMeshStack_ShouldHaveCorrectResourceCount()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            template.ResourceCountIs("AWS::AppMesh::Mesh", 1);
            template.ResourceCountIs("AWS::AppMesh::VirtualNode", 2); // Frontend and Backend
            template.ResourceCountIs("AWS::AppMesh::VirtualRouter", 1);
            // Note: There are 2 VirtualServices - one for backend and one for frontend
            template.ResourceCountIs("AWS::AppMesh::VirtualService", 2);
            template.ResourceCountIs("AWS::AppMesh::VirtualGateway", 1);
            template.ResourceCountIs("AWS::AppMesh::GatewayRoute", 1);
        }

        [Fact]
        public void AppMeshStack_ShouldConfigureTLSValidation()
        {
            var stack = new AppMeshStack(_app, "TestAppMeshStack", new StackProps(), _testConfig, _serviceNamespace);
            var template = Template.FromStack(stack);

            // Note: CloudFormation uses "TLS" (uppercase) not "Tls"
            template.HasResourceProperties("AWS::AppMesh::VirtualNode", new Dictionary<string, object>
            {
                ["Spec"] = Match.ObjectLike(new Dictionary<string, object>
                {
                    ["BackendDefaults"] = Match.ObjectLike(new Dictionary<string, object>
                    {
                        ["ClientPolicy"] = Match.ObjectLike(new Dictionary<string, object>
                        {
                            ["TLS"] = Match.ObjectLike(new Dictionary<string, object>
                            {
                                ["Validation"] = Match.ObjectLike(new Dictionary<string, object>())
                            })
                        })
                    })
                })
            });
        }
    }
}
