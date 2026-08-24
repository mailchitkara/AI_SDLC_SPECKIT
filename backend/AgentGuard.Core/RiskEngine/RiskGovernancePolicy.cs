namespace AgentGuard.Core.RiskEngine;

/// <summary>
/// FR-001, FR-003: the operator-configured set of risk dimensions for which any finding forces
/// the recommendation to at least HumanReviewRequired. Empty by default — no behavior change for
/// a deployment that hasn't configured this (016-mandatory-review-gate).
/// </summary>
public sealed class RiskGovernancePolicy
{
    public static readonly RiskGovernancePolicy Empty = new([]);

    public IReadOnlySet<RiskDimension> MandatoryReviewDimensions { get; }

    public RiskGovernancePolicy(IEnumerable<RiskDimension> mandatoryReviewDimensions)
    {
        MandatoryReviewDimensions = mandatoryReviewDimensions.ToHashSet();
    }
}
