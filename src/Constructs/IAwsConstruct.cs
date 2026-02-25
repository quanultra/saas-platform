using Constructs;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Constructs
{
    /// <summary>
    /// Base interface for all AWS constructs
    /// </summary>
    public interface IAwsConstruct
    {
        /// <summary>
        /// Get the construct ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Get tags for the construct
        /// </summary>
        Dictionary<string, string> Tags { get; }
    }

    /// <summary>
    /// Abstract base class for AWS constructs with common functionality
    /// </summary>
    public abstract class AwsConstructBase : Construct, IAwsConstruct
    {
        public string Id { get; }
        public Dictionary<string, string> Tags { get; protected set; }

        protected AwsConstructBase(Construct scope, string id, Dictionary<string, string> tags = null)
            : base(scope, id)
        {
            Id = id;
            Tags = tags ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Apply tags to a CDK resource
        /// </summary>
        protected void ApplyTagsToResource(Amazon.CDK.ITaggable resource)
        {
            foreach (var tag in Tags)
            {
                resource.Tags.SetTag(tag.Key, tag.Value);
            }
        }
    }
}
