using AgentGuard.Core;
using AgentGuard.Core.Dependencies;
using AgentGuard.Core.Findings;
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

    // RuleId is now a stable string-backed identity (005-risk-engine-foundation FR-001) — the API
    // string *is* the identity, so this is a passthrough rather than a switch over a closed enum.
    public static string ToApiString(this RuleId ruleId) => ruleId.Value;

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

    public static string ToApiString(this RiskDimension dimension) => dimension switch
    {
        RiskDimension.Security => "SECURITY",
        RiskDimension.Testing => "TESTING",
        RiskDimension.Compatibility => "COMPATIBILITY",
        RiskDimension.Architecture => "ARCHITECTURE",
        RiskDimension.ChangeManagement => "CHANGE_MANAGEMENT",
        RiskDimension.Dependencies => "DEPENDENCIES",
        RiskDimension.Reliability => "RELIABILITY",
        RiskDimension.Configuration => "CONFIGURATION",
        RiskDimension.BusinessCriticality => "BUSINESS_CRITICALITY",
        _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unknown risk dimension."),
    };

    public static string ToApiString(this Confidence confidence) => confidence switch
    {
        Confidence.Certain => "CERTAIN",
        Confidence.High => "HIGH",
        Confidence.Medium => "MEDIUM",
        Confidence.Low => "LOW",
        _ => throw new ArgumentOutOfRangeException(nameof(confidence), confidence, "Unknown confidence."),
    };

    public static string ToApiString(this FindingKind kind) => kind switch
    {
        FindingKind.Deterministic => "DETERMINISTIC",
        FindingKind.Contextual => "CONTEXTUAL",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown finding kind."),
    };

    public static bool TryParseExternalSeverity(string? value, out ExternalSeverity severity)
    {
        switch (value)
        {
            case "LOW": severity = ExternalSeverity.Low; return true;
            case "MODERATE": severity = ExternalSeverity.Moderate; return true;
            case "HIGH": severity = ExternalSeverity.High; return true;
            case "CRITICAL": severity = ExternalSeverity.Critical; return true;
            default: severity = default; return false;
        }
    }
}
