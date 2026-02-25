using AwsSapC02Practice.Infrastructure.Utils;
using FluentAssertions;
using System;
using Xunit;

namespace AwsSapC02Practice.Tests.Unit
{
    public class CostEstimatorTests
    {
        [Fact]
        public void AddCost_ShouldAddCostItem()
        {
            var estimator = new CostEstimator();

            estimator.AddCost("EC2", 100m, "t3.medium");

            var total = estimator.GetTotalMonthlyCost();
            total.Should().Be(100m);
        }

        [Fact]
        public void GetTotalMonthlyCost_ShouldSumAllCosts()
        {
            var estimator = new CostEstimator();

            estimator.AddCost("EC2", 100m);
            estimator.AddCost("RDS", 200m);
            estimator.AddCost("S3", 50m);

            var total = estimator.GetTotalMonthlyCost();
            total.Should().Be(350m);
        }

        [Fact]
        public void EstimateEc2Cost_ShouldCalculateCorrectly()
        {
            var cost = CostEstimator.EstimateEc2Cost("t3.micro", 2, 730);

            cost.Should().BeApproximately(15.18m, 0.01m);
        }

        [Fact]
        public void EstimateEc2Cost_ShouldThrowForUnknownInstanceType()
        {
            Action act = () => CostEstimator.EstimateEc2Cost("unknown.type");

            act.Should().Throw<ArgumentException>()
                .WithMessage("Unknown instance type: unknown.type");
        }

        [Fact]
        public void EstimateRdsCost_ShouldCalculateCorrectly()
        {
            var cost = CostEstimator.EstimateRdsCost("db.t3.micro", "postgres", 1);

            cost.Should().BeApproximately(12.41m, 0.01m);
        }

        [Fact]
        public void EstimateS3Cost_ShouldCalculateCorrectly()
        {
            var cost = CostEstimator.EstimateS3Cost(100, 10000);

            cost.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetCostBreakdown_ShouldReturnAllItems()
        {
            var estimator = new CostEstimator();
            estimator.AddCost("EC2", 100m);
            estimator.AddCost("RDS", 200m);

            var breakdown = estimator.GetCostBreakdown();

            breakdown.Should().HaveCount(2);
            breakdown.Should().ContainKey("EC2");
            breakdown.Should().ContainKey("RDS");
        }
    }
}
