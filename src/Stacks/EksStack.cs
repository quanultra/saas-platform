using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.EKS;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.LambdaLayer.KubectlV28;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// EKS Cluster Stack with managed node groups and IRSA
    /// Implements Requirement 8.11
    /// </summary>
    public class EksStack : BaseStack
    {
        public Cluster EksCluster { get; }
        public Nodegroup ManagedNodeGroup { get; }

        public EksStack(
            Construct scope,
            string id,
            IStackProps props,
            StackConfiguration config,
            IVpc vpc)
            : base(scope, id, props, config)
        {
            // Create IAM Role for EKS Cluster
            var clusterRole = new Role(this, "ClusterRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("eks.amazonaws.com"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonEKSClusterPolicy")
                }
            });

            // Create kubectl layer for EKS cluster
            var kubectlLayer = new KubectlV28Layer(this, "KubectlLayer");

            // Create EKS Cluster
            EksCluster = new Cluster(this, "EksCluster", new ClusterProps
            {
                ClusterName = GenerateResourceName("eks-cluster"),
                Version = KubernetesVersion.V1_28,
                Vpc = vpc,
                VpcSubnets = new[]
                {
                    new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS }
                },
                DefaultCapacity = 0, // We'll use managed node groups instead
                Role = clusterRole,
                EndpointAccess = EndpointAccess.PUBLIC_AND_PRIVATE,
                ClusterLogging = new[]
                {
                    ClusterLoggingTypes.API,
                    ClusterLoggingTypes.AUDIT,
                    ClusterLoggingTypes.AUTHENTICATOR,
                    ClusterLoggingTypes.CONTROLLER_MANAGER,
                    ClusterLoggingTypes.SCHEDULER
                },
                KubectlLayer = kubectlLayer
            });

            // Create IAM Role for Node Group
            var nodeRole = new Role(this, "NodeRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonEKSWorkerNodePolicy"),
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonEKS_CNI_Policy"),
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonEC2ContainerRegistryReadOnly"),
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMManagedInstanceCore")
                }
            });

            // Create Managed Node Group
            ManagedNodeGroup = EksCluster.AddNodegroupCapacity("ManagedNodeGroup", new NodegroupOptions
            {
                NodegroupName = GenerateResourceName("node-group"),
                InstanceTypes = new[]
                {
                    new InstanceType("t3.medium")
                },
                MinSize = 2,
                MaxSize = 10,
                DesiredSize = 2,
                DiskSize = 20,
                AmiType = NodegroupAmiType.AL2_X86_64,
                CapacityType = CapacityType.ON_DEMAND,
                NodeRole = nodeRole,
                Subnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                Labels = new Dictionary<string, string>
                {
                    ["environment"] = config.Environment,
                    ["nodegroup-type"] = "managed"
                },
                Tags = new Dictionary<string, string>
                {
                    ["k8s.io/cluster-autoscaler/enabled"] = "true",
                    [$"k8s.io/cluster-autoscaler/{GenerateResourceName("eks-cluster")}"] = "owned"
                }
            });


            // Create IRSA (IAM Roles for Service Accounts) for Cluster Autoscaler
            var autoscalerServiceAccount = EksCluster.AddServiceAccount("ClusterAutoscalerSA", new ServiceAccountOptions
            {
                Name = "cluster-autoscaler",
                Namespace = "kube-system"
            });

            // Add IAM policy for Cluster Autoscaler
            autoscalerServiceAccount.AddToPrincipalPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[]
                {
                    "autoscaling:DescribeAutoScalingGroups",
                    "autoscaling:DescribeAutoScalingInstances",
                    "autoscaling:DescribeLaunchConfigurations",
                    "autoscaling:DescribeScalingActivities",
                    "autoscaling:DescribeTags",
                    "ec2:DescribeInstanceTypes",
                    "ec2:DescribeLaunchTemplateVersions"
                },
                Resources = new[] { "*" }
            }));

            autoscalerServiceAccount.AddToPrincipalPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[]
                {
                    "autoscaling:SetDesiredCapacity",
                    "autoscaling:TerminateInstanceInAutoScalingGroup",
                    "ec2:DescribeImages",
                    "ec2:GetInstanceTypesFromInstanceRequirements",
                    "eks:DescribeNodegroup"
                },
                Resources = new[] { "*" }
            }));

            // Deploy Cluster Autoscaler using Helm
            var autoscalerChart = EksCluster.AddHelmChart("ClusterAutoscaler", new HelmChartOptions
            {
                Chart = "cluster-autoscaler",
                Repository = "https://kubernetes.github.io/autoscaler",
                Namespace = "kube-system",
                Release = "cluster-autoscaler",
                Values = new Dictionary<string, object>
                {
                    ["autoDiscovery"] = new Dictionary<string, object>
                    {
                        ["clusterName"] = EksCluster.ClusterName
                    },
                    ["awsRegion"] = props.Env?.Region ?? "us-east-1",
                    ["rbac"] = new Dictionary<string, object>
                    {
                        ["serviceAccount"] = new Dictionary<string, object>
                        {
                            ["create"] = false,
                            ["name"] = "cluster-autoscaler"
                        }
                    },
                    ["replicaCount"] = 1,
                    ["extraArgs"] = new Dictionary<string, object>
                    {
                        ["balance-similar-node-groups"] = true,
                        ["skip-nodes-with-system-pods"] = false
                    }
                }
            });

            // Create IRSA for AWS Load Balancer Controller
            var albControllerServiceAccount = EksCluster.AddServiceAccount("ALBControllerSA", new ServiceAccountOptions
            {
                Name = "aws-load-balancer-controller",
                Namespace = "kube-system"
            });

            // Add IAM policy for ALB Controller (simplified version)
            albControllerServiceAccount.AddToPrincipalPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[]
                {
                    "elasticloadbalancing:*",
                    "ec2:DescribeVpcs",
                    "ec2:DescribeSubnets",
                    "ec2:DescribeSecurityGroups",
                    "ec2:DescribeInstances",
                    "ec2:DescribeNetworkInterfaces",
                    "ec2:DescribeTags",
                    "ec2:CreateTags",
                    "ec2:DeleteTags"
                },
                Resources = new[] { "*" }
            }));

            // Create Outputs
            CreateOutput("ClusterName", EksCluster.ClusterName, "EKS Cluster Name");
            CreateOutput("ClusterArn", EksCluster.ClusterArn, "EKS Cluster ARN");
            CreateOutput("ClusterEndpoint", EksCluster.ClusterEndpoint, "EKS Cluster Endpoint");
            CreateOutput("ClusterSecurityGroupId", EksCluster.ClusterSecurityGroupId, "EKS Cluster Security Group ID");
            CreateOutput("NodeGroupName", ManagedNodeGroup.NodegroupName, "Managed Node Group Name");
            CreateOutput("AutoscalerServiceAccountName", autoscalerServiceAccount.ServiceAccountName, "Cluster Autoscaler Service Account");
        }
    }
}
