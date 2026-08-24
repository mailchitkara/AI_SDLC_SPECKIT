# Phase 1 Data Model: Mandatory Review Gate by Risk Dimension

## RiskGovernancePolicy (new, AgentGuard.Core.RiskEngine)

```csharp
public sealed class RiskGovernancePolicy
{
    public static readonly RiskGovernancePolicy Empty = new([]);

    public IReadOnlySet<RiskDimension> MandatoryReviewDimensions { get; }

    public RiskGovernancePolicy(IEnumerable<RiskDimension> mandatoryReviewDimensions) =>
        MandatoryReviewDimensions = mandatoryReviewDimensions.ToHashSet();
}
```

## ScoredRisk / RiskAnalysisResult (changed)

```csharp
public readonly record struct ScoredRisk(
    int Score,
    RiskClassification Classification,
    Recommendation Recommendation,
    bool RecommendationForcedByOverride,
    bool RecommendationForcedByGovernancePolicy);   // new

public sealed record RiskAnalysisResult(
    ..., // unchanged existing fields
    bool RecommendationForcedByOverride,
    bool RecommendationForcedByGovernancePolicy,    // new
    ...);
```

## RiskEngine.Evaluate (changed)

```
Evaluate(findings, thresholds = null, governancePolicy = null) -> ScoredRisk

score = ...                                    // unchanged
classification = ...                           // unchanged
forcedByOverride = findings.Any(MandatoryOverride)
preFloorRecommendation = forcedByOverride ? BlockMerge : RecommendationFor(classification)

matchesGovernedDimension = findings.Any(f => (governancePolicy ?? Empty).MandatoryReviewDimensions.Contains(f.Dimension))
forcedByGovernancePolicy = matchesGovernedDimension && preFloorRecommendation < HumanReviewRequired   // research.md §3
finalRecommendation = forcedByGovernancePolicy ? HumanReviewRequired : preFloorRecommendation

return ScoredRisk(score, classification, finalRecommendation, forcedByOverride, forcedByGovernancePolicy)
```

## Policy file JSON shape (015-policy-as-code, extended)

```json
{
  "forbiddenDependencies": [...],
  "businessCriticalPaths": [...],
  "mandatoryReviewDimensions": ["BUSINESS_CRITICALITY"]
}
```

`mandatoryReviewDimensions` is a new, optional, third top-level array — absent is equivalent to empty, matching the other two sections' existing precedent. Each string MUST be one of the recognized `RiskDimension` wire-format names `EnumMappings.ToApiString` already defines (e.g. `SECURITY`, `TESTING`, ..., `BUSINESS_CRITICALITY`); an unrecognized value fails startup loudly (research.md §5).

## AgentGuardAnalyzer constructor (changed)

```csharp
public AgentGuardAnalyzer(
    ForbiddenDependencyConfig? forbiddenDependencyConfig = null,
    BusinessCriticalPathConfig? businessCriticalPathConfig = null,
    RiskGovernancePolicy? riskGovernancePolicy = null)
```

Third optional parameter, defaulting to `.Empty`, passed through to `RiskEngine.Evaluate` — purely additive, no existing call site breaks.

## State / lifecycle note

None of this is persisted — loaded once at startup from the same file `015-policy-as-code` already reads, matching every other config in this codebase.
