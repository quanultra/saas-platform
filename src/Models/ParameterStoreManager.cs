using Amazon.CDK.AWS.SSM;
using Constructs;
using System.Collections.Generic;

namespace AwsSapC02Practice.Infrastructure.Models
{
    /// <summary>
    /// Manages AWS Systems Manager Parameter Store values
    /// </summary>
    public class ParameterStoreManager
    {
        private readonly Construct _scope;
        private readonly string _environment;
        private readonly Dictionary<string, IStringParameter> _parameters = new();

        public ParameterStoreManager(Construct scope, string environment)
        {
            _scope = scope;
            _environment = environment;
        }

        /// <summary>
        /// Create or update a parameter in Parameter Store
        /// </summary>
        public IStringParameter CreateParameter(string name, string value, string description = null)
        {
            var parameter = new StringParameter(_scope, $"Param-{name}", new StringParameterProps
            {
                ParameterName = name,
                StringValue = value,
                Description = description ?? $"Parameter for {_environment} environment",
                Tier = ParameterTier.STANDARD
            });

            _parameters[name] = parameter;
            return parameter;
        }

        /// <summary>
        /// Create a secure parameter in Parameter Store
        /// </summary>
        public IStringParameter CreateSecureParameter(string name, string value, string description = null)
        {
            var parameter = new StringParameter(_scope, $"SecureParam-{name}", new StringParameterProps
            {
                ParameterName = name,
                StringValue = value,
                Description = description ?? $"Secure parameter for {_environment} environment",
                Tier = ParameterTier.STANDARD,
                Type = ParameterType.SECURE_STRING
            });

            _parameters[name] = parameter;
            return parameter;
        }

        /// <summary>
        /// Get a parameter by name
        /// </summary>
        public IStringParameter GetParameter(string name)
        {
            if (_parameters.TryGetValue(name, out var parameter))
            {
                return parameter;
            }
            return null;
        }

        /// <summary>
        /// Create all parameters from environment config
        /// </summary>
        public void CreateParametersFromConfig(EnvironmentConfig config)
        {
            foreach (var kvp in config.ParameterStoreValues)
            {
                CreateParameter(kvp.Key, kvp.Value);
            }
        }
    }
}
