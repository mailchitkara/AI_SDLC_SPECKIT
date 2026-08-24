using AgentGuard.Core.Findings;
using AgentGuard.Core.Rules;
using FluentAssertions;
using Xunit;
using RiskEngineUnderTest = AgentGuard.Core.RiskEngine.RiskEngine;
using Severity = AgentGuard.Core.RiskEngine.Severity;
using SeverityWeights = AgentGuard.Core.RiskEngine.SeverityWeights;
using RiskClassification = AgentGuard.Core.RiskEngine.RiskClassification;
using Recommendation = AgentGuard.Core.RiskEngine.Recommendation;
using RiskDimension = AgentGuard.Core.RiskEngine.RiskDimension;
using Confidence = AgentGuard.Core.RiskEngine.Confidence;
using ThresholdConfiguration = AgentGuard.Core.RiskEngine.ThresholdConfiguration;

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
        var findings = severities.Select(s => MakeFinding(s)).ToList();

        var result = RiskEngineUnderTest.Evaluate(findings);

        result.Score.Should().Be(expectedScore);
        result.Classification.Should().Be(expectedClassification);
        result.Recommendation.Should().Be(expectedRecommendation);
    }

    [Fact]
    public void Evaluate_caps_the_score_at_100_when_summed_weights_exceed_it()
    {
        // Three HIGH findings sum to 105 raw weight, with no BLOCKER present.
        var findings = new[] { Severity.High, Severity.High, Severity.High }.Select(s => MakeFinding(s)).ToList();

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

    // --- 005-risk-engine-foundation: configurable thresholds (US2, T018) ---

    [Fact]
    public void Evaluate_with_no_thresholds_matches_V1s_fixed_default_bands()
    {
        var findings = new[] { Severity.Medium, Severity.Medium }.Select(s => MakeFinding(s)).ToList(); // score 40

        var result = RiskEngineUnderTest.Evaluate(findings);

        result.Classification.Should().Be(RiskClassification.Medium); // 25-49 under the default bands
    }

    [Fact]
    public void Evaluate_with_custom_thresholds_reclassifies_the_same_score()
    {
        var findings = new[] { Severity.Medium, Severity.Medium }.Select(s => MakeFinding(s)).ToList(); // score 40
        var narrowMedium = new ThresholdConfiguration(LowMax: 10, MediumMax: 20, HighMax: 74);

        var result = RiskEngineUnderTest.Evaluate(findings, narrowMedium);

        result.Score.Should().Be(40); // score arithmetic itself is unchanged (FR-009)
        result.Classification.Should().Be(RiskClassification.High); // but 40 now falls above MediumMax=20
    }

    // --- 005-risk-engine-foundation: mandatory override (US3, T024) ---

    [Fact]
    public void Evaluate_forces_block_merge_when_a_finding_has_mandatory_override_regardless_of_score()
    {
        var findings = new[] { MakeFinding(Severity.Low, mandatoryOverride: true) }; // score 10 alone would be LOW

        var result = RiskEngineUnderTest.Evaluate(findings);

        result.Recommendation.Should().Be(Recommendation.BlockMerge);
        result.RecommendationForcedByOverride.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_does_not_force_block_merge_when_no_finding_has_mandatory_override()
    {
        var findings = new[] { MakeFinding(Severity.Low) };

        var result = RiskEngineUnderTest.Evaluate(findings);

        result.Recommendation.Should().Be(Recommendation.SafeToReview);
        result.RecommendationForcedByOverride.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_identifies_which_finding_triggered_a_mandatory_override()
    {
        var overriding = MakeFinding(Severity.Low, mandatoryOverride: true);
        var findings = new[] { MakeFinding(Severity.Medium), overriding };

        var result = RiskEngineUnderTest.Evaluate(findings);

        // FR-011: the result must make the triggering finding(s) identifiable, not just that a block occurred.
        result.RecommendationForcedByOverride.Should().BeTrue();
        findings.Where(f => f.MandatoryOverride).Should().ContainSingle().Which.Should().BeSameAs(overriding);
    }

    // --- 016-mandatory-review-gate ---

    [Fact]
    public void Evaluate_floors_recommendation_at_human_review_required_when_a_finding_matches_a_governed_dimension()
    {
        var findings = new[] { MakeFinding(Severity.Low, dimension: RiskDimension.BusinessCriticality) }; // score 10 alone would be SafeToReview
        var policy = new RiskEngine.RiskGovernancePolicy([RiskDimension.BusinessCriticality]);

        var result = RiskEngineUnderTest.Evaluate(findings, governancePolicy: policy);

        result.Recommendation.Should().Be(Recommendation.HumanReviewRequired);
        result.RecommendationForcedByGovernancePolicy.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_leaves_recommendation_unchanged_when_no_governance_policy_is_configured()
    {
        var findings = new[] { MakeFinding(Severity.Low, dimension: RiskDimension.BusinessCriticality) };

        var result = RiskEngineUnderTest.Evaluate(findings);

        result.Recommendation.Should().Be(Recommendation.SafeToReview);
        result.RecommendationForcedByGovernancePolicy.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_does_not_attribute_block_merge_to_the_governance_policy_when_mandatory_override_already_caused_it()
    {
        var findings = new[] { MakeFinding(Severity.Low, mandatoryOverride: true, dimension: RiskDimension.BusinessCriticality) };
        var policy = new RiskEngine.RiskGovernancePolicy([RiskDimension.BusinessCriticality]);

        var result = RiskEngineUnderTest.Evaluate(findings, governancePolicy: policy);

        result.Recommendation.Should().Be(Recommendation.BlockMerge);
        result.RecommendationForcedByOverride.Should().BeTrue();
        result.RecommendationForcedByGovernancePolicy.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_does_not_attribute_human_review_required_to_the_policy_when_score_already_reached_it_alone()
    {
        var findings = new[] { MakeFinding(Severity.High), MakeFinding(Severity.High), MakeFinding(Severity.High, dimension: RiskDimension.BusinessCriticality) }; // score 100+ -> already HumanReviewRequired or higher on its own
        var policy = new RiskEngine.RiskGovernancePolicy([RiskDimension.BusinessCriticality]);

        var result = RiskEngineUnderTest.Evaluate(findings, governancePolicy: policy);

        result.RecommendationForcedByGovernancePolicy.Should().BeFalse();
    }

    private static Finding MakeFinding(Severity severity, bool mandatoryOverride = false, RiskDimension dimension = RiskDimension.Security) =>
        new(
            RuleCatalog.SecretDetected.Id,
            "Test Rule",
            severity,
            "explanation",
            "evidence",
            null,
            "remediation",
            dimension,
            Confidence.Certain,
            FindingKind.Deterministic,
            mandatoryOverride);
}
