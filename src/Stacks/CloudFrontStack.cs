using Amazon.CDK;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.S3;
using Constructs;
using AwsSapC02Practice.Infrastructure.Models;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// CloudFront Distribution Stack
    /// Implements Requirement 3.6
    /// </summary>
    public class CloudFrontStack : BaseStack
    {
        public IDistribution Distribution { get; }
        public IBucket LogBucket { get; }

        public CloudFrontStack(
            Construct scope,
            string id,
            IStackProps props,
            StackConfiguration config,
            IBucket originBucket)
            : base(scope, id, props, config)
        {
            // Create S3 bucket for CloudFront logs
            LogBucket = new Bucket(this, "CloudFrontLogBucket", new BucketProps
            {
                BucketName = GenerateResourceName("cloudfront-logs"),
                Encryption = BucketEncryption.S3_MANAGED,
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                RemovalPolicy = RemovalPolicy.DESTROY,
                AutoDeleteObjects = true,
                LifecycleRules = new[]
                {
                    new LifecycleRule
                    {
                        Id = "DeleteOldLogs",
                        Enabled = true,
                        Expiration = Duration.Days(90)
                    }
                }
            });

            // Create S3 origin with Origin Access Control
            var s3Origin = S3BucketOrigin.WithOriginAccessControl(originBucket);

            // Create cache policy for optimal caching
            var cachePolicy = new CachePolicy(this, "CachePolicy", new CachePolicyProps
            {
                CachePolicyName = GenerateResourceName("cache-policy"),
                Comment = "Cache policy for multi-region application",
                DefaultTtl = Duration.Hours(24),
                MinTtl = Duration.Seconds(0),
                MaxTtl = Duration.Days(365),
                CookieBehavior = CacheCookieBehavior.None(),
                HeaderBehavior = CacheHeaderBehavior.AllowList("CloudFront-Viewer-Country"),
                QueryStringBehavior = CacheQueryStringBehavior.All(),
                EnableAcceptEncodingGzip = true,
                EnableAcceptEncodingBrotli = true
            });

            // Create response headers policy with security headers
            var responseHeadersPolicy = new ResponseHeadersPolicy(this, "ResponseHeadersPolicy", new ResponseHeadersPolicyProps
            {
                ResponseHeadersPolicyName = GenerateResourceName("security-headers"),
                Comment = "Security headers for multi-region application",
                SecurityHeadersBehavior = new ResponseSecurityHeadersBehavior
                {
                    StrictTransportSecurity = new ResponseHeadersStrictTransportSecurity
                    {
                        AccessControlMaxAge = Duration.Days(365),
                        IncludeSubdomains = true,
                        Override = true
                    },
                    ContentTypeOptions = new ResponseHeadersContentTypeOptions
                    {
                        Override = true
                    },
                    FrameOptions = new ResponseHeadersFrameOptions
                    {
                        FrameOption = HeadersFrameOption.DENY,
                        Override = true
                    },
                    XssProtection = new ResponseHeadersXSSProtection
                    {
                        Protection = true,
                        ModeBlock = true,
                        Override = true
                    },
                    ReferrerPolicy = new ResponseHeadersReferrerPolicy
                    {
                        ReferrerPolicy = HeadersReferrerPolicy.STRICT_ORIGIN_WHEN_CROSS_ORIGIN,
                        Override = true
                    }
                }
            });

            // Create CloudFront distribution
            Distribution = new Distribution(this, "Distribution", new DistributionProps
            {
                Comment = $"CloudFront distribution for {config.ProjectName} {config.Environment}",
                DefaultBehavior = new BehaviorOptions
                {
                    Origin = s3Origin,
                    ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                    AllowedMethods = AllowedMethods.ALLOW_GET_HEAD_OPTIONS,
                    CachedMethods = CachedMethods.CACHE_GET_HEAD_OPTIONS,
                    CachePolicy = cachePolicy,
                    ResponseHeadersPolicy = responseHeadersPolicy,
                    Compress = true
                },
                EnableLogging = true,
                LogBucket = LogBucket,
                LogFilePrefix = "cloudfront-logs/",
                LogIncludesCookies = true,
                PriceClass = PriceClass.PRICE_CLASS_100, // Use only North America and Europe
                HttpVersion = HttpVersion.HTTP2_AND_3,
                MinimumProtocolVersion = SecurityPolicyProtocol.TLS_V1_2_2021,
                EnableIpv6 = true,
                DefaultRootObject = "index.html"
            });

            // Create outputs
            CreateOutput(
                "DistributionId",
                Distribution.DistributionId,
                "CloudFront Distribution ID"
            );

            CreateOutput(
                "DistributionDomainName",
                Distribution.DistributionDomainName,
                "CloudFront Distribution Domain Name"
            );

            CreateOutput(
                "LogBucketName",
                LogBucket.BucketName,
                "CloudFront Log Bucket Name"
            );

            // Add tags
            Tags.SetTag("Component", "CloudFront");
            Tags.SetTag("Service", "CDN");
        }
    }
}
