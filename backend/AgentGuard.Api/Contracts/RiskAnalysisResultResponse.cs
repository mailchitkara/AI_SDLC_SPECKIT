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
    string Remediation);

public sealed record CheckResultResponse(string RuleId, string RuleName, bool Passed);

public sealed record RiskAnalysisResultResponse(
    string RepositoryName,
    int PrNumber,
    string PrTitle,
    int Score,
    string Classification,
    string Recommendation,
    IReadOnlyList<CheckResultResponse> Checks,
    IReadOnlyList<FindingResponse> Findings);

public sealed record ValidationErrorResponse(string Message, IReadOnlyList<string> Errors);

public static class RiskAnalysisResultResponseMapping
{
    public static RiskAnalysisResultResponse ToResponse(this RiskAnalysisResult result) => new(
        RepositoryName: result.RepositoryName,
        PrNumber: result.PrNumber,
        PrTitle: result.PrTitle,
        Score: result.Score,
        Classification: result.Classification.ToApiString(),
        Recommendation: result.Recommendation.ToApiString(),
        Checks: result.Checks.Select(c => new CheckResultResponse(c.RuleId.ToApiString(), c.RuleName, c.Passed)).ToList(),
        Findings: result.Findings.Select(f => new FindingResponse(
                f.RuleId.ToApiString(),
                f.RuleName,
                f.Severity.ToApiString(),
                f.Explanation,
                f.Evidence,
                f.Location,
                f.Remediation))
            .ToList());
}
