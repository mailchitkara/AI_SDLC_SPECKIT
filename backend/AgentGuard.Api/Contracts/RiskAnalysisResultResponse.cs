using AgentGuard.Core.Findings;
using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Api.Contracts;

public sealed record FindingResponse(
    string RuleId,
    string RuleName,
    string Severity,
    string Explanation,
    string Evidence,
    string? Location,
    string Remediation,
    string Dimension,
    string Confidence,
    string Kind,
    bool MandatoryOverride);

public sealed record CheckResultResponse(string RuleId, string RuleName, bool Passed);

/// <summary>data-model.md: a file whose content GitHub could not serve inline (FR-009).</summary>
public sealed record PartiallyEvaluatedFileResponse(string Path, string Reason);

public sealed record RiskAnalysisResultResponse(
    string RepositoryName,
    int PrNumber,
    string PrTitle,
    int Score,
    string Classification,
    string Recommendation,
    bool RecommendationForcedByOverride,
    bool RecommendationForcedByGovernancePolicy,
    IReadOnlyList<CheckResultResponse> Checks,
    IReadOnlyList<FindingResponse> Findings,
    IReadOnlyList<PartiallyEvaluatedFileResponse> PartiallyEvaluatedFiles);

public sealed record ValidationErrorResponse(string Message, IReadOnlyList<string> Errors);

public static class RiskAnalysisResultResponseMapping
{
    public static RiskAnalysisResultResponse ToResponse(
        this RiskAnalysisResult result,
        IReadOnlyList<PartiallyEvaluatedFileResponse>? partiallyEvaluatedFiles = null) => new(
        RepositoryName: result.RepositoryName,
        PrNumber: result.PrNumber,
        PrTitle: result.PrTitle,
        Score: result.Score,
        Classification: result.Classification.ToApiString(),
        Recommendation: result.Recommendation.ToApiString(),
        RecommendationForcedByOverride: result.RecommendationForcedByOverride,
        RecommendationForcedByGovernancePolicy: result.RecommendationForcedByGovernancePolicy,
        Checks: result.Checks.Select(c => new CheckResultResponse(c.RuleId.ToApiString(), c.RuleName, c.Passed)).ToList(),
        Findings: result.Findings.Select(f => new FindingResponse(
                f.RuleId.ToApiString(),
                f.RuleName,
                f.Severity.ToApiString(),
                f.Explanation,
                f.Evidence,
                f.Location,
                f.Remediation,
                f.Dimension.ToApiString(),
                f.Confidence.ToApiString(),
                f.Kind.ToApiString(),
                f.MandatoryOverride))
            .ToList(),
        PartiallyEvaluatedFiles: partiallyEvaluatedFiles ?? []);
}
