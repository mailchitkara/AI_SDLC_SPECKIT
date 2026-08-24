using AgentGuard.Core.Findings;

namespace AgentGuard.Core.RiskEngine;

public readonly record struct ScoredRisk(
    int Score,
    RiskClassification Classification,
    Recommendation Recommendation,
    bool RecommendationForcedByOverride,
    bool RecommendationForcedByGovernancePolicy);

/// <summary>
/// Pure function: (findings, thresholds) -> (score, classification, recommendation). No I/O, no
/// randomness, no time dependency — required so identical input always produces an identical
/// result (FR-013). Assumes an already-valid ThresholdConfiguration; validation happens at the
/// API contracts layer (005-risk-engine-foundation research.md §4), not here.
/// </summary>
public static class RiskEngine
{
    public static ScoredRisk Evaluate(
        IReadOnlyList<Finding> findings,
        ThresholdConfiguration? thresholds = null,
        RiskGovernancePolicy? governancePolicy = null)
    {
        var effectiveThresholds = thresholds ?? ThresholdConfiguration.Default;
        var effectiveGovernancePolicy = governancePolicy ?? RiskGovernancePolicy.Empty;

        // FR-012 + FR-013: sum severity weights, capped at 100 — unchanged by this feature (FR-009).
        var score = Math.Min(100, findings.Sum(f => SeverityWeights.WeightOf(f.Severity)));

        var classification = ClassificationFor(score, effectiveThresholds);

        // FR-010/FR-012: a mandatory-override finding forces BLOCK_MERGE regardless of score/bands.
        var forcedByOverride = findings.Any(f => f.MandatoryOverride);
        var preFloorRecommendation = forcedByOverride
            ? Recommendation.BlockMerge
            : RecommendationFor(classification);

        // 016-mandatory-review-gate: a floor applied after scoring/override, never a ceiling and
        // never lower than what the pipeline already produced (research.md §1, §3).
        var matchesGovernedDimension = findings.Any(f => effectiveGovernancePolicy.MandatoryReviewDimensions.Contains(f.Dimension));
        var forcedByGovernancePolicy = matchesGovernedDimension && preFloorRecommendation < Recommendation.HumanReviewRequired;
        var recommendation = forcedByGovernancePolicy ? Recommendation.HumanReviewRequired : preFloorRecommendation;

        return new ScoredRisk(score, classification, recommendation, forcedByOverride, forcedByGovernancePolicy);
    }

    // FR-007: classification bands are configurable; V1's fixed bands are just the default.
    private static RiskClassification ClassificationFor(int score, ThresholdConfiguration thresholds) => score switch
    {
        _ when score <= thresholds.LowMax => RiskClassification.Low,
        _ when score <= thresholds.MediumMax => RiskClassification.Medium,
        _ when score <= thresholds.HighMax => RiskClassification.High,
        _ => RiskClassification.Critical,
    };

    // FR-016 (from 001-pr-risk-analysis-v1): fixed classification -> recommendation mapping, unchanged.
    private static Recommendation RecommendationFor(RiskClassification classification) => classification switch
    {
        RiskClassification.Low => Recommendation.SafeToReview,
        RiskClassification.Medium => Recommendation.ReviewRecommended,
        RiskClassification.High => Recommendation.HumanReviewRequired,
        RiskClassification.Critical => Recommendation.BlockMerge,
        _ => throw new ArgumentOutOfRangeException(nameof(classification), classification, "Unknown classification."),
    };
}
