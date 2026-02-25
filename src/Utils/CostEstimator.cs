using System;
using System.Collections.Generic;
using System.Linq;

namespace AwsSapC02Practice.Infrastructure.Utils
{
    /// <summary>
    /// Utility class for estimating AWS costs
    /// </summary>
    public class CostEstimator
    {
        private readonly Dictionary<string, decimal> _monthlyCosts = new();

        /// <summary>
        /// Add a cost item
        /// </summary>
        public void AddCost(string resourceType, decimal monthlyCost, string description = null)
        {
            var key = string.IsNullOrEmpty(description)
                ? resourceType
                : $"{resourceType}-{description}";

            _monthlyCosts[key] = monthlyCost;
        }

        /// <summary>
        /// Get total monthly cost
        /// </summary>
        public decimal GetTotalMonthlyCost()
        {
            return _monthlyCosts.Values.Sum();
        }

        /// <summary>
        /// Get cost breakdown by resource type
        /// </summary>
        public Dictionary<string, decimal> GetCostBreakdown()
        {
            return new Dictionary<string, decimal>(_monthlyCosts);
        }

        /// <summary>
        /// Get formatted cost summary
        /// </summary>
        public string GetCostSummary()
        {
            var total = GetTotalMonthlyCost();
            var breakdown = string.Join("\n", _monthlyCosts.Select(kvp =>
                $"  - {kvp.Key}: ${kvp.Value:F2}/month"));

            return $"Estimated Monthly Cost: ${total:F2}\n\nBreakdown:\n{breakdown}";
        }

        /// <summary>
        /// Estimate EC2 instance cost
        /// </summary>
        public static decimal EstimateEc2Cost(string instanceType, int count = 1, int hoursPerMonth = 730)
        {
            // Simplified pricing - actual prices vary by region
            var hourlyRates = new Dictionary<string, decimal>
            {
                ["t3.micro"] = 0.0104m,
                ["t3.small"] = 0.0208m,
                ["t3.medium"] = 0.0416m,
                ["t3.large"] = 0.0832m,
                ["m5.large"] = 0.096m,
                ["m5.xlarge"] = 0.192m,
                ["c5.large"] = 0.085m,
                ["r5.large"] = 0.126m
            };

            if (!hourlyRates.TryGetValue(instanceType, out var rate))
            {
                throw new ArgumentException($"Unknown instance type: {instanceType}");
            }

            return rate * hoursPerMonth * count;
        }

        /// <summary>
        /// Estimate RDS cost
        /// </summary>
        public static decimal EstimateRdsCost(string instanceClass, string engine = "postgres", int count = 1)
        {
            // Simplified pricing
            var hourlyRates = new Dictionary<string, decimal>
            {
                ["db.t3.micro"] = 0.017m,
                ["db.t3.small"] = 0.034m,
                ["db.t3.medium"] = 0.068m,
                ["db.r5.large"] = 0.24m,
                ["db.r5.xlarge"] = 0.48m
            };

            if (!hourlyRates.TryGetValue(instanceClass, out var rate))
            {
                throw new ArgumentException($"Unknown instance class: {instanceClass}");
            }

            return rate * 730 * count;
        }

        /// <summary>
        /// Estimate S3 storage cost
        /// </summary>
        public static decimal EstimateS3Cost(int storageGb, int requestsPerMonth = 10000)
        {
            var storageCost = storageGb * 0.023m; // Standard storage
            var requestCost = (requestsPerMonth / 1000m) * 0.0004m; // PUT/POST requests
            return storageCost + requestCost;
        }
    }
}
