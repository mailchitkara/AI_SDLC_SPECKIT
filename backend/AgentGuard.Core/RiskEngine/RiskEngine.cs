using AgentGuard.Core.Findings;

namespace AgentGuard.Core.RiskEngine;

public readonly record struct ScoredRisk(int Score, RiskClassification Classification, Recommendation Recommendation);

/// <summary>
/// Pure function: (findings) -> (score, classification, recommendation). No I/O, no randomness,
/// no time dependency — required so identical input always produces an identical result (FR-013).
/// </summary>
public static class RiskEngine
{
    public static ScoredRisk Evaluate(IReadOnlyList<Finding> findings)
    {
        // FR-012 + FR-013: sum severity weights, capped at 100.
        var score = Math.Min(100, findings.Sum(f => SeverityWeights.WeightOf(f.Severity)));

        var classification = ClassificationFor(score);
        var recommendation = RecommendationFor(classification);

        return new ScoredRisk(score, classification, recommendation);
    }

    // FR-015: fixed score bands.
    private static RiskClassification ClassificationFor(int score) => score switch
    {
        <= 24 => RiskClassification.Low,
        <= 49 => RiskClassification.Medium,
        <= 74 => RiskClassification.High,
        _ => RiskClassification.Critical,
    };

    // FR-016: fixed classification -> recommendation mapping.
    private static Recommendation RecommendationFor(RiskClassification classification) => classification switch
    {
        RiskClassification.Low => Recommendation.SafeToReview,
        RiskClassification.Medium => Recommendation.ReviewRecommended,
        RiskClassification.High => Recommendation.HumanReviewRequired,
        RiskClassification.Critical => Recommendation.BlockMerge,
        _ => throw new ArgumentOutOfRangeException(nameof(classification), classification, "Unknown classification."),
    };
}
