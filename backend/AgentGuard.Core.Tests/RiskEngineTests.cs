using AgentGuard.Core.Findings;
using AgentGuard.Core.Rules;
using FluentAssertions;
using Xunit;
using RiskEngineUnderTest = AgentGuard.Core.RiskEngine.RiskEngine;
using Severity = AgentGuard.Core.RiskEngine.Severity;
using SeverityWeights = AgentGuard.Core.RiskEngine.SeverityWeights;
using RiskClassification = AgentGuard.Core.RiskEngine.RiskClassification;
using Recommendation = AgentGuard.Core.RiskEngine.Recommendation;

namespace AgentGuard.Core.Tests;

public class RiskEngineTests
{
    [Theory]
    [InlineData(Severity.Info, 0)]
    [InlineData(Severity.Low, 10)]
    [InlineData(Severity.Medium, 20)]
    [InlineData(Severity.High, 35)]
    [InlineData(Severity.Blocker, 100)]
    public void WeightOf_matches_the_fixed_severity_weight_table(Severity severity, int expectedWeight)
    {
        SeverityWeights.WeightOf(severity).Should().Be(expectedWeight);
    }

    [Fact]
    public void Evaluate_with_no_findings_yields_score_zero_low_safe_to_review()
    {
        var result = RiskEngineUnderTest.Evaluate([]);

        result.Score.Should().Be(0);
        result.Classification.Should().Be(RiskClassification.Low);
        result.Recommendation.Should().Be(Recommendation.SafeToReview);
    }

    [Theory]
    [InlineData(new[] { Severity.Low }, 10, RiskClassification.Low, Recommendation.SafeToReview)]
    [InlineData(new[] { Severity.Medium, Severity.Medium }, 40, RiskClassification.Medium, Recommendation.ReviewRecommended)]
    [InlineData(new[] { Severity.High, Severity.High }, 70, RiskClassification.High, Recommendation.HumanReviewRequired)]
    public void Evaluate_derives_classification_and_recommendation_from_summed_weights(
        Severity[] severities, int expectedScore, RiskClassification expectedClassification, Recommendation expectedRecommendation)
    {
        var findings = severities.Select(MakeFinding).ToList();

        var result = RiskEngineUnderTest.Evaluate(findings);

        result.Score.Should().Be(expectedScore);
        result.Classification.Should().Be(expectedClassification);
        result.Recommendation.Should().Be(expectedRecommendation);
    }

    [Fact]
    public void Evaluate_caps_the_score_at_100_when_summed_weights_exceed_it()
    {
        // Three HIGH findings sum to 105 raw weight, with no BLOCKER present.
        var findings = new[] { Severity.High, Severity.High, Severity.High }.Select(MakeFinding).ToList();

        var result = RiskEngineUnderTest.Evaluate(findings);

        result.Score.Should().Be(100);
        result.Classification.Should().Be(RiskClassification.Critical);
        result.Recommendation.Should().Be(Recommendation.BlockMerge);
    }

    [Fact]
    public void Evaluate_with_any_blocker_finding_always_yields_score_100_critical_block_merge()
    {
        // A BLOCKER finding alongside low-severity noise must still resolve to the fixed invariant (FR-014, FR-017).
        var findings = new[]
        {
            MakeFinding(Severity.Low),
            MakeFinding(Severity.Blocker),
        };

        var result = RiskEngineUnderTest.Evaluate(findings);

        result.Score.Should().Be(100);
        result.Classification.Should().Be(RiskClassification.Critical);
        result.Recommendation.Should().Be(Recommendation.BlockMerge);
    }

    private static Finding MakeFinding(Severity severity) =>
        new(RuleId.SecretDetected, "Test Rule", severity, "explanation", "evidence", null, "remediation");
}
