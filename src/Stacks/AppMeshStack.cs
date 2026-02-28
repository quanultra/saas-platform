using Amazon.CDK;
using Amazon.CDK.AWS.AppMesh;
using Amazon.CDK.AWS.ServiceDiscovery;
using Amazon.CDK.AWS.IAM;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    public class AppMeshStack : BaseStack
    {
        public IMesh Mesh { get; }
        public IVirtualNode FrontendVirtualNode { get; }
        public IVirtualNode BackendVirtualNode { get; }
        public IVirtualRouter BackendVirtualRouter { get; }
        public IVirtualService BackendVirtualService { get; }

        public AppMeshStack(
            Construct scope,
            string id,
            IStackProps props,
            StackConfiguration config,
            IPrivateDnsNamespace serviceNamespace)
            : base(scope, id, props, config)
        {
            Mesh = new Mesh(this, "AppMesh", new MeshProps
            {
                MeshName = GenerateResourceName("mesh"),
                EgressFilter = MeshFilterType.ALLOW_ALL
            });

            var backendService = new Amazon.CDK.AWS.ServiceDiscovery.Service(this, "BackendService", new Amazon.CDK.AWS.ServiceDiscovery.ServiceProps
            {
                Name = "backend",
                Namespace = serviceNamespace,
                DnsRecordType = DnsRecordType.A,
                DnsTtl = Duration.Seconds(60),
                RoutingPolicy = RoutingPolicy.MULTIVALUE
            });

            BackendVirtualNode = new VirtualNode(this, "BackendVirtualNode", new VirtualNodeProps
            {
                Mesh = Mesh,
                VirtualNodeName = "backend-service",
                ServiceDiscovery = ServiceDiscovery.CloudMap(backendService),
                Listeners = new[]
                {
                    VirtualNodeListener.Http(new HttpVirtualNodeListenerOptions
                    {
                        Port = 8080,
                        HealthCheck = HealthCheck.Http(new HttpHealthCheckOptions
                        {
                            HealthyThreshold = 2,
                            UnhealthyThreshold = 3,
                            Timeout = Duration.Seconds(5),
                            Interval = Duration.Seconds(30),
                            Path = "/health"
                        }),
                        Timeout = new HttpTimeout
                        {
                            Idle = Duration.Seconds(60),
                            PerRequest = Duration.Seconds(30)
                        }
                    })
                },
                AccessLog = AccessLog.FromFilePath("/dev/stdout"),
                BackendDefaults = new BackendDefaults
                {
                    TlsClientPolicy = new TlsClientPolicy
                    {
                        Validation = new TlsValidation
                        {
                            Trust = TlsValidationTrust.File("/etc/ssl/certs/ca-bundle.crt")
                        }
                    }
                }
            });

            BackendVirtualRouter = new VirtualRouter(this, "BackendVirtualRouter", new VirtualRouterProps
            {
                Mesh = Mesh,
                VirtualRouterName = "backend-router",
                Listeners = new[]
                {
                    VirtualRouterListener.Http(8080)
                }
            });

            var backendRoute = BackendVirtualRouter.AddRoute("BackendRoute", new RouteBaseProps
            {
                RouteName = "backend-route",
                RouteSpec = RouteSpec.Http(new HttpRouteSpecOptions
                {
                    WeightedTargets = new[]
                    {
                        new WeightedTarget
                        {
                            VirtualNode = BackendVirtualNode,
                            Weight = 100
                        }
                    },
                    RetryPolicy = new HttpRetryPolicy
                    {
                        RetryAttempts = 3,
                        RetryTimeout = Duration.Seconds(10),
                        HttpRetryEvents = new[]
                        {
                            HttpRetryEvent.SERVER_ERROR,
                            HttpRetryEvent.GATEWAY_ERROR
                        },
                        TcpRetryEvents = new[]
                        {
                            TcpRetryEvent.CONNECTION_ERROR
                        }
                    },
                    Timeout = new HttpTimeout
                    {
                        Idle = Duration.Seconds(60),
                        PerRequest = Duration.Seconds(30)
                    }
                })
            });

            BackendVirtualService = new VirtualService(this, "BackendVirtualService", new VirtualServiceProps
            {
                VirtualServiceName = $"backend.{serviceNamespace.NamespaceName}",
                VirtualServiceProvider = VirtualServiceProvider.VirtualRouter(BackendVirtualRouter)
            });

            var frontendService = new Amazon.CDK.AWS.ServiceDiscovery.Service(this, "FrontendService", new Amazon.CDK.AWS.ServiceDiscovery.ServiceProps
            {
                Name = "frontend",
                Namespace = serviceNamespace,
                DnsRecordType = DnsRecordType.A,
                DnsTtl = Duration.Seconds(60),
                RoutingPolicy = RoutingPolicy.MULTIVALUE
            });

            FrontendVirtualNode = new VirtualNode(this, "FrontendVirtualNode", new VirtualNodeProps
            {
                Mesh = Mesh,
                VirtualNodeName = "frontend-service",
                ServiceDiscovery = ServiceDiscovery.CloudMap(frontendService),
                Listeners = new[]
                {
                    VirtualNodeListener.Http(new HttpVirtualNodeListenerOptions
                    {
                        Port = 80,
                        HealthCheck = HealthCheck.Http(new HttpHealthCheckOptions
                        {
                            HealthyThreshold = 2,
                            UnhealthyThreshold = 3,
                            Timeout = Duration.Seconds(5),
                            Interval = Duration.Seconds(30),
                            Path = "/health"
                        })
                    })
                },
                Backends = new[]
                {
                    Backend.VirtualService(BackendVirtualService)
                },
                AccessLog = AccessLog.FromFilePath("/dev/stdout")
            });

            var envoyRole = new Role(this, "EnvoyProxyRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                Description = "IAM role for Envoy proxy with X-Ray tracing",
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("AWSAppMeshEnvoyAccess"),
                    ManagedPolicy.FromAwsManagedPolicyName("AWSXRayDaemonWriteAccess")
                }
            });

            envoyRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[]
                {
                    "appmesh:StreamAggregatedResources",
                    "servicediscovery:DiscoverInstances"
                },
                Resources = new[] { "*" }
            }));

            var virtualGateway = new VirtualGateway(this, "VirtualGateway", new VirtualGatewayProps
            {
                Mesh = Mesh,
                VirtualGatewayName = "ingress-gateway",
                Listeners = new[]
                {
                    VirtualGatewayListener.Http(new HttpGatewayListenerOptions
                    {
                        Port = 80,
                        HealthCheck = HealthCheck.Http(new HttpHealthCheckOptions
                        {
                            HealthyThreshold = 2,
                            UnhealthyThreshold = 3,
                            Timeout = Duration.Seconds(5),
                            Interval = Duration.Seconds(30),
                            Path = "/health"
                        })
                    })
                },
                AccessLog = AccessLog.FromFilePath("/dev/stdout"),
                BackendDefaults = new BackendDefaults
                {
                    TlsClientPolicy = new TlsClientPolicy
                    {
                        Validation = new TlsValidation
                        {
                            Trust = TlsValidationTrust.File("/etc/ssl/certs/ca-bundle.crt")
                        }
                    }
                }
            });

            var frontendVirtualService = new VirtualService(this, "FrontendVirtualService", new VirtualServiceProps
            {
                VirtualServiceName = $"frontend.{serviceNamespace.NamespaceName}",
                VirtualServiceProvider = VirtualServiceProvider.VirtualNode(FrontendVirtualNode)
            });

            var gatewayRoute = new GatewayRoute(this, "GatewayRoute", new GatewayRouteProps
            {
                RouteSpec = GatewayRouteSpec.Http(new HttpGatewayRouteSpecOptions
                {
                    RouteTarget = frontendVirtualService,
                    Match = new HttpGatewayRouteMatch
                    {
                        Path = HttpGatewayRoutePathMatch.StartsWith("/")
                    }
                }),
                VirtualGateway = virtualGateway,
                GatewayRouteName = "default-route"
            });

            CreateOutput("MeshName", Mesh.MeshName, "App Mesh Name");
            CreateOutput("MeshArn", Mesh.MeshArn, "App Mesh ARN");
            CreateOutput("FrontendVirtualNodeName", FrontendVirtualNode.VirtualNodeName, "Frontend Virtual Node Name");
            CreateOutput("BackendVirtualNodeName", BackendVirtualNode.VirtualNodeName, "Backend Virtual Node Name");
            CreateOutput("BackendVirtualServiceName", BackendVirtualService.VirtualServiceName, "Backend Virtual Service Name");
            CreateOutput("VirtualGatewayName", virtualGateway.VirtualGatewayName, "Virtual Gateway Name");
            CreateOutput("EnvoyRoleArn", envoyRole.RoleArn, "Envoy Proxy IAM Role ARN");
        }
    }
}
