using AgentGuard.Core.Findings;

namespace AgentGuard.Core.RiskEngine;

public enum RiskClassification
{
    Low,
    Medium,
    High,
    Critical,
}

public enum Recommendation
{
    SafeToReview,
    ReviewRecommended,
    HumanReviewRequired,
    BlockMerge,
}

public sealed record RiskAnalysisResult(
    string RepositoryName,
    int PrNumber,
    string PrTitle,
    int Score,
    RiskClassification Classification,
    Recommendation Recommendation,
    IReadOnlyList<CheckResult> Checks,
    IReadOnlyList<Finding> Findings);
