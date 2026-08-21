using AgentGuard.Core;
using AgentGuard.Core.Rules;
using AgentGuard.Core.RiskEngine;

namespace AgentGuard.Api.Contracts;

/// <summary>
/// Explicit, two-way mapping between AgentGuard.Core's C# enums and the exact SCREAMING_SNAKE_CASE
/// string values defined in contracts/openapi.yaml. Kept in the API layer, not Core, since JSON
/// wire-format naming is an API concern (constitution: UI/wire-format concerns MUST NOT leak into Core).
/// </summary>
public static class EnumMappings
{
    public static bool TryParseChangeType(string? value, out ChangeType changeType)
    {
        switch (value)
        {
            case "ADDED": changeType = ChangeType.Added; return true;
            case "MODIFIED": changeType = ChangeType.Modified; return true;
            case "DELETED": changeType = ChangeType.Deleted; return true;
            case "RENAMED": changeType = ChangeType.Renamed; return true;
            default: changeType = default; return false;
        }
    }

    public static string ToApiString(this RuleId ruleId) => ruleId switch
    {
        RuleId.LargeChangeSize => "LARGE_CHANGE_SIZE",
        RuleId.MissingRelatedTests => "MISSING_RELATED_TESTS",
        RuleId.ApiContractBreakingChange => "API_CONTRACT_BREAKING_CHANGE",
        RuleId.ArchitectureViolation => "ARCHITECTURE_VIOLATION",
        RuleId.SecretDetected => "SECRET_DETECTED",
        _ => throw new ArgumentOutOfRangeException(nameof(ruleId), ruleId, "Unknown rule id."),
    };

    public static string ToApiString(this Severity severity) => severity switch
    {
        Severity.Info => "INFO",
        Severity.Low => "LOW",
        Severity.Medium => "MEDIUM",
        Severity.High => "HIGH",
        Severity.Blocker => "BLOCKER",
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown severity."),
    };

    public static string ToApiString(this RiskClassification classification) => classification switch
    {
        RiskClassification.Low => "LOW",
        RiskClassification.Medium => "MEDIUM",
        RiskClassification.High => "HIGH",
        RiskClassification.Critical => "CRITICAL",
        _ => throw new ArgumentOutOfRangeException(nameof(classification), classification, "Unknown classification."),
    };

    public static string ToApiString(this Recommendation recommendation) => recommendation switch
    {
        Recommendation.SafeToReview => "SAFE_TO_REVIEW",
        Recommendation.ReviewRecommended => "REVIEW_RECOMMENDED",
        Recommendation.HumanReviewRequired => "HUMAN_REVIEW_REQUIRED",
        Recommendation.BlockMerge => "BLOCK_MERGE",
        _ => throw new ArgumentOutOfRangeException(nameof(recommendation), recommendation, "Unknown recommendation."),
    };
}
