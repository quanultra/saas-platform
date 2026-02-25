using Amazon.CDK;
using Constructs;
using System.Collections.Generic;
using AwsSapC02Practice.Infrastructure.Models;

namespace AwsSapC02Practice.Infrastructure.Stacks
{
    /// <summary>
    /// Base stack class with common functionality for all stacks
    /// </summary>
    public abstract class BaseStack : Stack
    {
        protected StackConfiguration Config { get; }
        protected Dictionary<string, string> CommonTags { get; }

        protected BaseStack(Construct scope, string id, IStackProps props, StackConfiguration config)
            : base(scope, id, props)
        {
            Config = config ?? new StackConfiguration();
            CommonTags = BuildCommonTags();
            ApplyTags();
        }

        /// <summary>
        /// Build common tags for all resources
        /// </summary>
        private Dictionary<string, string> BuildCommonTags()
        {
            var tags = new Dictionary<string, string>
            {
                ["Project"] = Config.ProjectName,
                ["Environment"] = Config.Environment,
                ["ManagedBy"] = "CDK",
                ["CostCenter"] = "Training"
            };

            // Merge with custom tags from config
            foreach (var tag in Config.Tags)
            {
                tags[tag.Key] = tag.Value;
            }

            return tags;
        }

        /// <summary>
        /// Apply common tags to the stack
        /// </summary>
        private void ApplyTags()
        {
            foreach (var tag in CommonTags)
            {
                Tags.SetTag(tag.Key, tag.Value);
            }
        }

        /// <summary>
        /// Create a CloudFormation output
        /// </summary>
        protected void CreateOutput(string id, string value, string description = null)
        {
            new CfnOutput(this, id, new CfnOutputProps
            {
                Value = value,
                Description = description,
                ExportName = $"{Config.ProjectName}-{Config.Environment}-{id}"
            });
        }

        /// <summary>
        /// Generate a resource name with consistent naming convention
        /// </summary>
        protected string GenerateResourceName(string resourceType)
        {
            return $"{Config.ProjectName}-{Config.Environment}-{resourceType}";
        }
    }
}
